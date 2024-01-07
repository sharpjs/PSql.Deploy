// Copyright 2024 Subatomix Research Inc.
// SPDX-License-Identifier: ISC

using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using DependencyQueue;

namespace PSql.Deploy.Seeding;

using static Environment;
using static RuntimeInformation;

using Queue                = DependencyQueue                     <SeedModule>;
using QueueContext         = DependencyQueueContext              <SeedModule, ISeedSession>;
using QueueError           = DependencyQueueError                ;
using QueueErrorType       = DependencyQueueErrorType            ;
using CycleError           = DependencyQueueCycleError           <SeedModule>;
using UnprovidedTopicError = DependencyQueueUnprovidedTopicError <SeedModule>;

/// <summary>
///   An algorithm that applies a content seed to a target database.
/// </summary>
internal class SeedApplicator : IDisposable
{
    private readonly ISeedSession   _session;
    private readonly LoadedSeed     _seed;
    private readonly SqlContextWork _target;
    private readonly TextWriter     _writer;
    private readonly Stopwatch      _stopwatch;

    // Count of modules successfully completed
    private int _appliedCount;

    // Outcome
    private TargetDisposition _disposition;

    /// <summary>
    ///   Initializes a new <see cref="SeedApplicator"/> instance.
    /// </summary>
    /// <param name="session">
    ///   The seed application session.
    /// </param>
    /// <param name="seed">
    ///   The seed to apply.
    /// </param>
    /// <param name="target">
    ///   An object specifying how to connect to the target database.
    /// </param>
    /// <exception cref="ArgumentNullException">
    ///   <paramref name="session"/>,
    ///   <paramref name="seed"/>, and/or
    ///   <paramref name="target"/> is <see langword="null"/>.
    /// </exception>
    public SeedApplicator(ISeedSession session, LoadedSeed seed, SqlContextWork target)
    {
        if (session is null)
            throw new ArgumentNullException(nameof(session));
        if (seed is null)
            throw new ArgumentNullException(nameof(seed));
        if (target is null)
            throw new ArgumentNullException(nameof(target));

        _session     = session;
        _seed        = seed;
        _target      = target;

        SqlStrategy  = Deploy.SqlStrategy.GetInstance(_session.IsWhatIfMode);

        _writer      = TextWriter.Synchronized(session.CreateLog(seed.Seed, target));

        _stopwatch   = Stopwatch.StartNew();
    }

    /// <inheritdoc cref="Seed.Name"/>
    public string SeedName => _seed.Seed.Name;

    /// <inheritdoc cref="SqlContextWork.ServerDisplayName"/>
    public string ServerName => _target.ServerDisplayName;

    /// <inheritdoc cref="SqlContextWork.DatabaseDisplayName"/>
    public string DatabaseName => _target.DatabaseDisplayName;

    /// <inheritdoc cref="SqlContextWork.Context"/>
    public SqlContext Context => _target.Context;

    /// <inheritdoc cref="SeedSession.Console"/>
    public ISeedConsole Console => _session.Console;

    // Mockable boundary for testability
    internal ISqlStrategy SqlStrategy { get; set; }

    /// <summary>
    ///   Applies the seed to the target database asynchronously.
    /// </summary>
    /// <returns>
    ///   A <see cref="Task"/> representing the asynchronous operation.
    /// </returns>
    /// <exception cref="SeedException">
    ///   One or more validation errors was reported.
    /// </exception>
    public async Task ApplyAsync()
    {
        try
        {
            ReportStarting();
            ReportModules();

            using var queue = new Queue(StringComparer.OrdinalIgnoreCase);

            Populate(queue);
            Validate(queue);

            await queue.RunAsync(
                SeedWorkerMainAsync,
                _session,
                _session.MaxParallelism,
                _session.CancellationToken
            );
        }
        catch (OperationCanceledException)
        {
            _disposition = TargetDisposition.Incomplete;
            ReportCanceled();
            throw;
        }
        catch (Exception e)
        {
            _disposition = TargetDisposition.Failed;
            ReportException(e);
            throw;
        }
        finally
        {
            ReportEnded();
        }
    }

    private void Populate(DependencyQueue<SeedModule> queue)
    {
        var builder = queue.CreateEntryBuilder();

        foreach (var module in _seed.Modules)
        {
            builder
                .NewEntry(module.Name, module)
                .AddProvides(module.Provides)
                .AddRequires(module.Requires)
                .Enqueue();
        }
    }

    private void Validate(DependencyQueue<SeedModule> queue)
    {
        var errors = queue.Validate();

        ReportDiagnostics(errors);

        if (errors.Count > 0)
            throw OnInvalid();
    }

    private async Task SeedWorkerMainAsync(QueueContext context)
    {
        using var connection = await ConnectAsync(context);
        using var command    = connection.CreateCommand();

        await PrepareAsync(command, context);

        while (await context.GetNextEntryAsync() is { Value: var module })
            await ExecuteAsync(module, command, context);
    }

    private Task<ISqlConnection> ConnectAsync(QueueContext context)
    {
        var logger = new SeedSqlMessageLogger(_writer, context.WorkerId);

        return SqlStrategy.ConnectAsync(Context, logger, _session.CancellationToken);
    }

    private Task PrepareAsync(ISqlCommand command, QueueContext context)
    {
        command.CommandText = string.Create(
            CultureInfo.InvariantCulture,
            $"""
            -- PrepareAsync
            DECLARE
                @RunId    uniqueidentifier = '{context.RunId:D}',
                @WorkerId int              = '{context.WorkerId:D}';

            SET CONTEXT_INFO @RunId;

            EXEC sp_set_session_context N'RunId',    @RunId,    @read_only = 1;
            EXEC sp_set_session_context N'WorkerId', @WorkerId, @read_only = 1;
            """
        );

        return SqlStrategy.ExecuteNonQueryAsync(command, context.CancellationToken);
    }

    private async Task ExecuteAsync(SeedModule module, ISqlCommand command, QueueContext context)
    {
        ReportApplying(module, context.WorkerId);

        foreach (var batch in module.Batches)
            await ExecuteAsync(batch, command, context);

        Interlocked.Increment(ref _appliedCount);
    }

    private Task ExecuteAsync(string batch, ISqlCommand command, QueueContext context)
    {
        command.CommandText = batch;

        return SqlStrategy.ExecuteNonQueryAsync(command, context.CancellationToken);
    }

    private void ReportStarting()
    {
        _session.Console.ReportStarting();

        var process = Process.GetCurrentProcess();

        Log("PSql.Deploy Seed Log");
        Log("");
        Log($"Seed:               {SeedName}");
        Log($"Target Server:      {ServerName}");
        Log($"Target Database:    {DatabaseName}");
        Log($"Start Time:         {DateTime.UtcNow:o}");
        Log($"Machine:            {MachineName}");
        Log($"Logical CPUs:       {ProcessorCount}");
        Log($"User:               {UserName}");
        Log($"Process:            {process.Id} {process.ProcessName} ({ProcessArchitecture})");
        Log($"Operating System:   {OSDescription} ({OSArchitecture})");
        Log($".NET Runtime:       {FrameworkDescription}");
    }

    private void ReportNoModules()
    {
        Log("");
        Log($"Seed Modules: 0");
        Log("");
        Log("Nothing to do.");
    }

    private void ReportModules()
    {
        // A contrived example showing nearly all combinations of output:
        //
        // Seed Modules: 4
        //
        //                    SEED MODULES
        //
        // NAME            PROVIDES         REQUIRES
        // ════════════════════════════════════════════════
        // Tenant          Membership       (none)
        // User            Membership       (none)
        // TenantUser      Membership       Tenant
        //                                  User
        // Role            Membership       (none)
        //                 Rbac
        // TenantUserRole  Membership       TenantUser
        //                 Rbac             Role

        const string
            Title          = "SEED MODULES",
            NameHeader     = "NAME",
            ProvidesHeader = "PROVIDES",
            RequiresHeader = "REQUIRES";
        
        var nameColumn     = new Column(NameHeader);
        var providesColumn = new Column(ProvidesHeader);
        var requiresColumn = new Column(RequiresHeader);

        foreach (var module in _seed.Modules)
        {
            nameColumn.Fit(module.Name);

            foreach (var name in module.Provides)
                providesColumn.Fit(name);

            foreach (var name in module.Requires)
                requiresColumn.Fit(name);
        }

        var tableWidth = nameColumn.Width + providesColumn.Width + requiresColumn.Width + 4;

        Log("");
        Log($"Seed Modules: {_seed.Modules.Length}");
        Log("");

        // Table header row 1
        Log($"{Space.Center(Title, tableWidth).Left}{Title}");
        Log("");

        // Table header row 2
        Log(string.Format(
            "{0}{1}  {2}{3}  {4}",
            NameHeader,     nameColumn    .GetPadding(NameHeader),
            ProvidesHeader, providesColumn.GetPadding(ProvidesHeader),
            RequiresHeader
        ));

        // Table header row 3
        Log(new string('═', tableWidth));

        // Body
        foreach (var module in _seed.Modules)
        {
            var provides = module.Provides.GetEnumerator();
            var requires = module.Requires.GetEnumerator();

            var name     = module.Name;
            var provided = provides.MoveNext() ? provides.Current : "(none)";
            var required = requires.MoveNext() ? requires.Current : "(none)";

            for (;;)
            {
                Log(string.Format(
                    "{0}{1}  {2}{3}  {4}",
                    name,     nameColumn    .GetPadding(name),
                    provided, providesColumn.GetPadding(provided),
                    required
                ));

                var hasProvides = provides.MoveNext();
                var hasRequires = provides.MoveNext();
                if (!hasProvides && !hasRequires)
                    break;

                name     = "";
                provided = hasProvides ? provides.Current : "";
                required = hasRequires ? requires.Current : "";
            }
        }
    }

    private void ReportDiagnostics(IReadOnlyCollection<QueueError> errors)
    {
        // Header
        Log("");
        Log("Validation Results:");
        Log("");

        // Content
        if (errors.Count == 0)
            Log("The seed is valid.");
        else
            foreach (var error in errors)
                ReportDiagnostic(error);
    }

    private void ReportDiagnostic(QueueError error)
    {
        var message = error.Type switch
        {
            QueueErrorType.UnprovidedTopic => Format((UnprovidedTopicError) error),
            _          /* .Cycle */        => Format((CycleError)           error),
        };

        Console.ReportProblem(message);

        Log($"Error: {message}");
    }

    private string Format(UnprovidedTopicError error)
    {
        return string.Format(
            "The topic '{0}' is required but not provided by any module. " +
            "Ensure that a module exists named '{0}' or that one or more " +
            "modules provide the topic '{0}'.",
            error.Topic.Name
        );
    }

    private string Format(CycleError error)
    {
        return string.Format(
            "The module '{0}' cannot require the topic '{1}' because " +
            "a module providing '{1}' already requires '{0}'. " +
            "The dependency graph does not permit cycles.",
            error.RequiringEntry.Name,
            error.RequiredTopic .Name
        );
    }

    private void ReportCanceled()
    {
        Log("");
        Log("Seed application was canceled.");
    }

    private void ReportException(Exception exception)
    {
        Console.ReportProblem(exception.Message);

        Log(exception.ToString());
    }

    private void ReportApplying(SeedModule module, int workerId)
    {
        Console.ReportApplying(module.Name);

        Log($"{workerId}> [{module.Name}]");
    }

    private void ReportEnded()
    {
        var count   = Volatile.Read(ref _appliedCount);
        var elapsed = _stopwatch.Elapsed;

        Console.ReportApplied(count, elapsed, _disposition);

        // Footer
        Log("");
        Log($"Applied {count} modules(s) in {elapsed.TotalSeconds:N3} second(s)");
    }

    /// <summary>
    ///   Writes the specified text and a line ending to the per-database log
    ///   file.
    /// </summary>
    /// <param name="text">
    ///   The text to write.
    /// </param>
    public void Log(string text)
        => _writer.WriteLine(text);

    private static SeedException OnInvalid()
        => new("The seed is invalid. Correct the validation errors and retry.");

    /// <inheritdoc/>
    public void Dispose()
    {
        _writer.Dispose();
    }
}
