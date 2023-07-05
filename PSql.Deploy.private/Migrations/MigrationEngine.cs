// Copyright 2023 Subatomix Research Inc.
// SPDX-License-Identifier: ISC

using System.Collections.Immutable;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace PSql.Deploy.Migrations;

/// <summary>
///   An engine that applies a set of migrations to a set of target databases.
/// </summary>
public class MigrationEngine
{
    /// <summary>
    ///   Initializes a new <see cref="MigrationEngine"/> instance.
    /// </summary>
    /// <param name="console">
    ///   The console on which to display status and important messages.
    /// </param>
    /// <param name="logPath">
    ///   The path of a directory in which to save per-database log files.
    /// </param>
    /// <param name="cancellation">
    ///   The token to monitor for cancellation requests.
    /// </param>
    /// <exception cref="ArgumentNullException">
    ///   <paramref name="console"/> and/or
    ///   <paramref name="logPath"/> is <see langword="null"/>.
    /// </exception>
    public MigrationEngine(IConsole console, string logPath, CancellationToken cancellation)
    {
        if (console is null)
            throw new ArgumentNullException(nameof(console));
        if (logPath is null)
            throw new ArgumentNullException(nameof(logPath));

        _totalTime           = Stopwatch.StartNew();

        Migrations           = ImmutableArray<Migration>.Empty;
        MinimumMigrationName = "";

        Console              = console;
        LogPath              = logPath;
        CancellationToken    = cancellation;

        Directory.CreateDirectory(LogPath);
    }

    /// <summary>
    ///   Gets the migrations to be applied to target databases.  The default
    ///   value is an empty array.
    /// </summary>
    /// <remarks>
    ///   Invoke <see cref="DiscoverMigrations"/> to populate this property.
    /// </remarks>
    public ImmutableArray<Migration> Migrations { get; private set; }

    /// <summary>
    ///   Gets the minimum name of the non-pseudo migrations to be applied to
    ///   target databases, or the empty string if no such migration is known.
    ///   The default value is the empty string.
    /// </summary>
    /// <remarks>
    ///   Invoke <see cref="DiscoverMigrations"/> to populate this property.
    /// </remarks>
    public string MinimumMigrationName { get; private set; }

    /// <summary>
    ///   Gets or sets the phase of migrations to be applied to target
    ///   databases.
    /// </summary>
    public MigrationPhase Phase { get; set; }

    /// <summary>
    ///   Gets the console on which to display status and important messages.
    /// </summary>
    public IConsole Console { get; }

    /// <summary>
    ///   Gets the path of a directory in which to save per-database log files.
    /// </summary>
    public string LogPath { get; }

    /// <summary>
    ///   Gets the token to monitor for cancellation requests.
    /// </summary>
    public CancellationToken CancellationToken { get; }

    // Time elapsed since construction
    private readonly Stopwatch _totalTime;

    // Whether any thread encountered an error
    private int _errorCount;

    /// <summary>
    ///   Discovers migrations in the specified directory path.
    /// </summary>
    /// <param name="path">
    ///   The path of a directory in which to discover migrations.
    /// </param>
    public void DiscoverMigrations(string path)
    {
        Migrations           = MigrationRepository.GetAll(path);
        MinimumMigrationName = Migrations.FirstOrDefault(m => !m.IsPseudo)?.Name ?? "";
    }

    /// <summary>
    ///   Applies migrations to the specified target databases asynchronously.
    /// </summary>
    /// <param name="contextSets">
    ///   The context sets specifying the target databases to which to apply
    ///   migrations.
    /// </param>
    /// <returns>
    ///   A <see cref="Task"/> representing the asynchronous operation.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    ///   <paramref name="contextSets"/> is <see langword="null"/>.
    /// </exception>
    public async Task ApplyAsync(IReadOnlyCollection<SqlContextParallelSet> contextSets)
    {
        if (contextSets is null)
            throw new ArgumentNullException(nameof(contextSets));

        if (contextSets.Count == 0)
            return;

        await Task.WhenAll(contextSets.Select(ApplyAsync));
    }

    /// <summary>
    ///   Applies migrations to the specified target databases asynchronously.
    /// </summary>
    /// <param name="contextSet">
    ///   The context set specifying the target databases to which to apply
    ///   migrations.
    /// </param>
    /// <returns>
    ///   A <see cref="Task"/> representing the asynchronous operation.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    ///   <paramref name="contextSet"/> is <see langword="null"/>.
    /// </exception>
    public async Task ApplyAsync(SqlContextParallelSet contextSet)
    {
        if (contextSet is null)
            throw new ArgumentNullException(nameof(contextSet));

        if (contextSet.Contexts.Count == 0)
            return;

        // Ensure parallelized when caller invokes in loop
        await Task.Yield();

        using var limiter = new SemaphoreSlim(contextSet.Parallelism, contextSet.Parallelism);

        async Task RunLimitedAsync(SqlContext target)
        {
            await limiter.WaitAsync(CancellationToken);
            try
            {
                await ApplyAsync(target);
            }
            finally
            {
                limiter.Release();
            }
        }

        await Task.WhenAll(contextSet.Contexts.Select(RunLimitedAsync));
    }

    /// <summary>
    ///   Applies migrations to the specified target database asynchronously.
    /// </summary>
    /// <param name="context">
    ///   The context specifying the target database to which to apply
    ///   migrations.
    /// </param>
    /// <returns>
    ///   A <see cref="Task"/> representing the asynchronous operation.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    ///   <paramref name="context"/> is <see langword="null"/>.
    /// </exception>
    public async Task ApplyAsync(SqlContext context)
    {
        if (context is null)
            throw new ArgumentNullException(nameof(context));

        // Ensure parallelized when caller invokes in loop
        await Task.Yield();

        using var target = new MigrationTarget(context, Phase, LogPath);

        ReportStarting(target);

        var targetMigrations = await GetTargetMigrationsAsync(target);
        var mergedMigrations = MergeSourceAndTargetMigrations(targetMigrations);

        if (mergedMigrations.All(m => m.IsPseudo))
        {
            // No migrations or only pseudo-migrations
            ReportNoMigrations(target);
            return;
        }

        ReportMigrations      (mergedMigrations, target);
        ValidateMigrations    (mergedMigrations, target); // throws if invalid
        var plan = ComputePlan(mergedMigrations);

        ReportPlan        (plan, target);
        await ExecuteAsync(plan, target);
    }

    private Task<IReadOnlyList<Migration>> GetTargetMigrationsAsync(MigrationTarget target)
    {
        return MigrationRepository.GetAllAsync(
            target.Target,     MinimumMigrationName,
            target.LogConsole, CancellationToken
        );
    }

    private ImmutableArray<Migration> MergeSourceAndTargetMigrations(
        IReadOnlyList<Migration> targetMigrations)
    {
        return new MigrationMerger(Phase)
            .Merge(Migrations.AsSpan(), targetMigrations);
    }

    private void ValidateMigrations(ImmutableArray<Migration> migrations, MigrationTarget target)
    {
        var isValid = new MigrationValidator(target, Phase, MinimumMigrationName, Console)
            .Validate(migrations.AsSpan());

        if (!isValid)
            throw new ApplicationException("Unable to perform migrations due to validation errors.");
    }

    private MigrationPlan ComputePlan(ImmutableArray<Migration> migrations)
    {
        return new MigrationPlanner(migrations.AsSpan())
            .CreatePlan();
    }

    private async Task ExecuteAsync(MigrationPlan plan, MigrationTarget target)
    {
        var items = plan.GetItems(Phase);
        if (!items.Any())
            return;

        using var connection = target.Connect();
        using var command    = connection.CreateCommand();

        command.CommandTimeout = 0; // No timeout

        var count     = 0;
        var exception = null as Exception;

        try
        {
            foreach (var (migration, phase) in items)
            {
                // Stop if another thread encountered an error
                if (_errorCount > 0)
                    return;

                // Prepare to run the item
                ReportApplying(migration, phase, target);
                connection.ClearErrors();

                // Run the item
                await ExecuteAsync(migration, phase, command);

                // Report errors if they happened
                connection.ThrowIfHasErrors();
                count++;
            }
        }
        catch (Exception e)
        {
            Interlocked.Increment(ref _errorCount);
            exception = e;
            throw;
        }
        finally
        {
            ReportApplied(count, target, exception);
        }
    }

    private Task ExecuteAsync(Migration migration, MigrationPhase phase, ISqlCommand command)
    {
        var sql = migration.GetSql(phase);
        if (sql.IsNullOrEmpty())
            return Task.CompletedTask;

        command.CommandText = sql;

        return command.UnderlyingCommand.ExecuteNonQueryAsync(CancellationToken);
    }

    private void ReportStarting(MigrationTarget target)
    {
        Console.WriteHost(string.Format(
            @"[+{0:hh\:mm\:ss}] {1}: Computing {2} migrations",
            _totalTime.Elapsed,
            target.DatabaseName,
            Phase
        ));

        target.Log("PSql.Deploy Migration Log");
        target.Log("");
        target.Log($"Migration Phase:    {Phase}");
        target.Log($"Target Server:      {target.ServerName}");
        target.Log($"Target Database:    {target.DatabaseName}");
        target.Log($"Start Time:         {DateTime.UtcNow:o}");
        target.Log($"Machine:            {Environment.MachineName}");
        target.Log($"Logical CPUs:       {Environment.ProcessorCount}");
        target.Log($"User:               {Environment.UserName}");
        target.Log($"Process:            {Process.GetCurrentProcess().Id} ({RuntimeInformation.ProcessArchitecture})");
        target.Log($"Operating System:   {RuntimeInformation.OSDescription} ({RuntimeInformation.OSArchitecture})");
        target.Log($".NET Runtime:       {RuntimeInformation.FrameworkDescription}");
    }

    private void ReportNoMigrations(MigrationTarget target)
    {
        target.Log("Migrations:         0");
        target.Log("");
        target.Log("Nothing to do.");
    }

    private void ReportMigrations(ImmutableArray<Migration> migrations, MigrationTarget target)
    {
        target.Log($"Migrations:         {migrations.Length}");
        target.Log("");

        var nameColumnWidth = 4;

        foreach (var migration in migrations)
            if (!migration.IsPseudo)
                nameColumnWidth = Math.Max(nameColumnWidth, migration.Name.Length);

        // NAME               FILES     PROGRESS          DEPENDS-ON
        // 2023-01-01-10042   Ok        (new)             (none)
        // 2023-01-02-10128   Missing   Pre->Core->Post   (none)
        // 2023-01-03-10420   Ok        Pre->Core         2023-01-01-10042
        // 2023-01-04-10777   Changed   Pre               (none)

        target.Log(string.Format(
            "NAME{0}   FILES     PROGRESS          DEPENDS-ON",
            new string(' ', nameColumnWidth - 4)
        ));

        foreach (var migration in migrations)
        {
            if (migration.IsPseudo)
                continue;

            target.Log(string.Format(
                "{0}{1}   {2}   {3}   {4}",
                migration.Name,
                new string(' ', nameColumnWidth - migration.Name.Length),
                migration.HasChanged switch
                {
                    true                                => "Changed",
                    false when migration.PreSql is null => "Missing",
                    _                                   => "Ok     ",
                },
                migration.State.ToFixedWidthProgressString(),
                migration.Depends?.LastOrDefault() ?? "(none)"
            ));
        }

        target.Log("");
    }

    private void ReportPlan(MigrationPlan plan, MigrationTarget target)
    {
        target.Log("Sequence:");
        target.Log("");

        var items = plan.GetItems(Phase);

        var nameColumnWidth = 4;

        foreach (var (migration, _) in items)
            nameColumnWidth = Math.Max(nameColumnWidth, migration.Name.Length);

        target.Log(string.Format(
            "NAME{0}   PHASE",
            new string(' ', nameColumnWidth - 4)
        ));

        foreach (var (migration, phase) in items)
            target.Log(string.Format(
                "{0}{1}   {2}",
                migration.Name,
                new string(' ', nameColumnWidth - migration.Name.Length),
                phase
            ));

        target.Log("");
    }

    private void ReportApplying(Migration migration, MigrationPhase phase, MigrationTarget target)
    {
        Console.WriteHost(string.Format(
            @"[+{0:hh\:mm\:ss}] {1}: Applying {2} {3}",
            _totalTime.Elapsed,
            target.DatabaseName,
            migration.Name,
            phase
        ));
    }

    private void ReportApplied(int count, MigrationTarget target, Exception? exception)
    {
        var abnormality = 
            exception is not null ? " [EXCEPTION]"  :
            _errorCount > 0       ? " [INCOMPLETE]" :
            null;

        Console.WriteHost(string.Format(
            @"[+{0:hh\:mm\:ss}] {1}: Applied {2} {3} item(s) in {4:N3} second(s){5}",
            _totalTime.Elapsed,
            target.DatabaseName,
            count,
            Phase,
            target.ElapsedTime.TotalSeconds,
            abnormality
        ));
    }
}
