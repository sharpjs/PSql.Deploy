// Copyright Subatomix Research Inc.
// SPDX-License-Identifier: MIT

using System.Diagnostics;
using DependencyQueue;

namespace PSql.Deploy.Seeds;

using Queue                = DependencyQueue                     <SeedModule>;
using QueueContext         = DependencyQueueContext              <SeedModule, ISeedSessionInternal>;
using QueueError           = DependencyQueueError                ;
using QueueErrorType       = DependencyQueueErrorType            ;
using CycleError           = DependencyQueueCycleError           <SeedModule>;
using UnprovidedTopicError = DependencyQueueUnprovidedTopicError <SeedModule>;

/// <summary>
///   An algorithm that applies a content seed to a target database.
/// </summary>
internal class SeedApplicator : ISeedApplication
{
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
    /// <param name="maxParallelism">
    ///   The maximum number of batches that may be executed against the target
    ///   database in parallel.
    /// </param>
    /// <exception cref="ArgumentNullException">
    ///   <paramref name="session"/>,
    ///   <paramref name="seed"/>, and/or
    ///   <paramref name="target"/> is <see langword="null"/>.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    ///   <paramref name="maxParallelism"/> is zero or negative.
    /// </exception>
    public SeedApplicator(
        ISeedSessionInternal session,
        LoadedSeed           seed,
        Target               target,
        int                  maxParallelism)
    {
        if (session is null)
            throw new ArgumentNullException(nameof(session));
        if (seed is null)
            throw new ArgumentNullException(nameof(seed));
        if (target is null)
            throw new ArgumentNullException(nameof(target));
        if (maxParallelism < 1)
            throw new ArgumentOutOfRangeException(nameof(maxParallelism));

        Session        = session;
        Seed           = seed;
        Target         = target;
        MaxParallelism = maxParallelism;

        _stopwatch = new();
    }

    /// <inheritdoc cref="ISeedApplication.Session"/>
    public ISeedSessionInternal Session { get; }

    /// <inheritdoc/>
    ISeedSession ISeedApplication.Session => Session;

    /// <summary>
    ///   Gets the user interface via which to report progress.
    /// </summary>
    public ISeedConsole Console => Session.Console;

    /// <summary>
    ///   Gets the seed being applied.
    /// </summary>
    public LoadedSeed Seed { get; }

    /// <summary>
    ///   Gets an object representing the target database.
    /// </summary>
    public Target Target { get; }

    /// <summary>
    ///   Gets the maximum number of batches that may be executed against the
    ///   target database in parallel.
    /// </summary>
    public int MaxParallelism { get; }

    // Time elapsed in ApplyAsync
    private readonly Stopwatch _stopwatch;

    // Count of modules successfully completed
    private int _appliedCount;

    // Outcome
    private TargetDisposition _disposition;

    // Log
    private TextWriter? _logWriter;

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
        BeginLog();

        try
        {
            ReportStarting();
            ReportModules();

            using var queue = new Queue(StringComparer.OrdinalIgnoreCase);

            Populate(queue);
            Validate(queue);

            ReportApplying();

            await queue.RunAsync(
                SeedWorkerMainAsync,
                Session,
                MaxParallelism,
                Session.CancellationToken
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
                  BestEffort.Do     (self => self.ReportEnded(),   this);
            await BestEffort.DoAsync(self => self.CloseLogAsync(), this);
        }
    }

    private void Populate(DependencyQueue<SeedModule> queue)
    {
        var builder = queue.CreateEntryBuilder();

        foreach (var module in Seed.Modules)
        {
            if (module.WorkerId is -1)
                for (var i = 1; i <= MaxParallelism; i++)
                    Populate(builder, Clone(module, workerId: i));
            else
                Populate(builder, module);
        }
    }

    private static void Populate(DependencyQueueEntryBuilder<SeedModule> builder, SeedModule module)
    {
        builder
            .NewEntry(module.Name, module)
            .AddProvides(module.Provides)
            .AddRequires(module.Requires)
            .Enqueue();
    }

    private static SeedModule Clone(SeedModule module, int workerId)
    {
        return new(module.Name, workerId, module.Batches, module.Provides, module.Requires);
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
        await using var connection = Connect(context);

        await PrepareAsync(connection, context);

        bool CanTake(SeedModule module)
            => module.WorkerId == 0
            || module.WorkerId == context.WorkerId;

        while (await context.GetNextEntryAsync(CanTake) is { Value: var module })
            await ExecuteAsync(module, connection, context);
    }

    private ISeedTargetConnection Connect(QueueContext context)
    {
        Assume.NotNull(_logWriter);

        var prefix = $"{context.WorkerId}>";
        var logger = new PrefixTextWriterSqlMessageLogger(_logWriter, prefix);

        return Session.Connect(Target, logger);
    }

    private static async Task PrepareAsync(ISeedTargetConnection connection, QueueContext context)
    {
        await connection.OpenAsync(context.CancellationToken);

        await connection.PrepareAsync(
            context.RunId,
            context.WorkerId,
            context.CancellationToken
        );
    }

    private async Task ExecuteAsync(SeedModule module, ISeedTargetConnection connection, QueueContext context)
    {
        ReportApplying(module, context.WorkerId);

        foreach (var batch in module.Batches)
            await connection.ExecuteSeedBatchAsync(batch, context.CancellationToken);

        Interlocked.Increment(ref _appliedCount);
    }

    private void ReportStarting()
    {
        _stopwatch.Restart();

        Console.ReportStarting(this);

        var i = ProcessInfo.Instance;

        Log("PSql.Deploy Seed Log");
        Log("");
        Log($"Seed:               {Seed.Seed.Name}");
        Log($"Target Server:      {Target.ServerDisplayName}");
        Log($"Target Database:    {Target.DatabaseDisplayName}");
        Log($"Start Time:         {DateTime.UtcNow:o}");
        Log($"Machine:            {i.MachineName}");
        Log($"Logical CPUs:       {i.ProcessorCount}");
        Log($"User:               {i.UserName}");
        Log($"Process:            {i.ProcessId} {i.ProcessName} ({i.ProcessArchitecture})");
        Log($"Operating System:   {i.OSDescription} ({i.OSArchitecture})");
        Log($".NET Runtime:       {i.FrameworkDescription}");
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

        foreach (var module in Seed.Modules)
        {
            nameColumn.Fit(module.Name);

            foreach (var name in module.Provides)
                providesColumn.Fit(name);

            foreach (var name in module.Requires)
                requiresColumn.Fit(name);
        }

        var tableWidth = nameColumn.Width + providesColumn.Width + requiresColumn.Width + 4;

        Log("");
        Log($"Seed Modules: {Seed.Modules.Length}");
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
        foreach (var module in Seed.Modules)
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
                var hasRequires = requires.MoveNext();
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
        if (errors.Count is 0)
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
            QueueErrorType.Cycle or _      => Format((CycleError)           error),
        };

        Console.ReportProblem(this, message);

        Log($"Error: {message}");
    }

    private static string Format(UnprovidedTopicError error)
    {
        return string.Format(
            "The topic '{0}' is required but not provided by any module. " +
            "Ensure that a module exists named '{0}' or that one or more " +
            "modules provide the topic '{0}'.",
            error.Topic.Name
        );
    }

    private static string Format(CycleError error)
    {
        return string.Format(
            "The module '{0}' cannot require the topic '{1}' because " +
            "a module providing '{1}' already requires '{0}'. " +
            "The dependency graph does not permit cycles.",
            error.RequiringEntry.Name,
            error.RequiredTopic .Name
        );
    }

    private void ReportApplying()
    {
        Log("");
        Log("Execution Log:");
        Log("");
    }

    private void ReportApplying(SeedModule module, int workerId)
    {
        Console.ReportApplying(this, module.Name);

        Log($"{workerId}> [{module.Name}]");
    }

    private void ReportCanceled()
    {
        Log("");
        Log("Seed application was canceled.");
    }

    private void ReportException(Exception exception)
    {
        Console.ReportProblem(this, exception.Message);

        // In the case of a SeedException, the applicator has already logged
        // helpful diagnostics; log the exception message only, as summary.
        // Other exceptions are truly unexpected; log full exception details.
        Log("");
        Log(exception is SeedException ? exception.Message : exception.ToString());
    }

    private void ReportEnded()
    {
        _stopwatch.Stop();

        var count   = Volatile.Read(ref _appliedCount);
        var elapsed = _stopwatch.Elapsed;

        Console.ReportApplied(this, count, elapsed, _disposition);

        Log("");
        Log($"Applied {count} modules(s) in {elapsed.TotalSeconds:N3} second(s).");
    }

    private void BeginLog()
    {
        _logWriter = Console.CreateLog(this);
    }

    private async ValueTask CloseLogAsync()
    {
        Assume.NotNull(_logWriter);

        await _logWriter.FlushAsync();
        await _logWriter.DisposeAsync();

        _logWriter = null;
    }

    /// <summary>
    ///   Writes the specified text and a line ending to the per-database log
    ///   file.
    /// </summary>
    /// <param name="text">
    ///   The text to write.
    /// </param>
    public void Log(string text)
    {
        Assume.NotNull(_logWriter);

        _logWriter.WriteLine(text);
    }

    private static SeedException OnInvalid()
    {
        return new("The seed is invalid. Correct the validation errors and retry.");
    }
}
