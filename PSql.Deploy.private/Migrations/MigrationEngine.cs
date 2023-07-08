// Copyright 2023 Subatomix Research Inc.
// SPDX-License-Identifier: ISC

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
    ///   Gets the context sets specifying the target databases to which to
    ///   apply migrations.
    /// </summary>
    /// <remarks>
    ///   Invoke <see cref="SpecifyTargets"/> to populate this property.
    /// </remarks>
    public ImmutableArray<SqlContextParallelSet> Targets { get; private set; }

    /// <summary>
    ///   Gets the phase of migrations to be applied to target databases.
    /// </summary>
    /// <remarks>
    ///   <see cref="ApplyAsync(MigrationPhase)"/> populates this propery.
    /// </remarks>
    public MigrationPhase Phase { get; private set; }

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

    // Dynamic column widths
    private int _databaseNameColumnWidth;
    private int _migrationNameColumnWidth;

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
    ///   Specifies the target databases to which to apply migrations.
    /// </summary>
    /// <param name="contextSets">
    ///   The context sets specifying the target databases to which to apply
    ///   migrations.
    /// </param>
    /// <exception cref="ArgumentNullException">
    ///   <paramref name="contextSets"/> is <see langword="null"/>.
    /// </exception>
    public void SpecifyTargets(IEnumerable<SqlContextParallelSet> contextSets)
    {
        if (contextSets is null)
            throw new ArgumentNullException(nameof(contextSets));

        Targets                  = contextSets.ToImmutableArray();
        _databaseNameColumnWidth = ComputeDatabaseNameColumnWidth(Targets);
    }

    /// <summary>
    ///   Applies migrations for the specified phase asynchronously.
    /// </summary>
    /// <param name="phase">
    ///   The phase of migrations to be applied to target databases.
    /// </param>
    /// <returns>
    ///   A <see cref="Task"/> representing the asynchronous operation.
    /// </returns>
    public Task ApplyAsync(MigrationPhase phase)
    {
        Phase = phase;

        if (Targets.Length == 0)
            return Task.CompletedTask;

        return Task.WhenAll(Targets.Select(ApplyAsync));
    }

    private async Task ApplyAsync(SqlContextParallelSet contextSet)
    {
        if (contextSet.Contexts.Count == 0)
            return;

        using var limiter = new SemaphoreSlim(
            initialCount: contextSet.Parallelism,
            maxCount:     contextSet.Parallelism
        );

        async Task RunLimitedAsync(SqlContext target)
        {
            await limiter.WaitAsync(CancellationToken);
            try
            {
                // Move to another thread so sibling task can start next context
                await Task.Yield();
                await ApplyAsync(target);
            }
            finally
            {
                limiter.Release();
            }
        }

        // Move to another thread so sibling task can start next context set
        await Task.Yield();
        await Task.WhenAll(contextSet.Contexts.Select(RunLimitedAsync));
    }

    private async Task ApplyAsync(SqlContext context)
    {
        using var target = new MigrationTarget(context, Phase, LogPath);

        ReportStarting(target);

        // 
        var targetMigrations = await GetTargetMigrationsAsync(target);
        var mergedMigrations = MergeSourceAndTargetMigrations(targetMigrations);

        // No migrations or only pseudo-migrations
        if (mergedMigrations.All(m => m.IsPseudo))
        {
            ReportNoMigrations(target);
            return;
        }

        _migrationNameColumnWidth = ComputeMigrationNameColumnWidth(mergedMigrations);

        ReportMigrations      (mergedMigrations, target);
        ValidateMigrations    (mergedMigrations, target); // throws if invalid
        var plan = ComputePlan(mergedMigrations);

        ReportPlan        (plan, target);
        await ExecuteAsync(plan, target);
    }

    private Task<IReadOnlyList<Migration>> GetTargetMigrationsAsync(MigrationTarget target)
    {
        return MigrationRepository.GetAllAsync(
            target.Context,    MinimumMigrationName,
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

    private static int ComputeDatabaseNameColumnWidth(
        IReadOnlyCollection<SqlContextParallelSet> contextSets)
    {
        const int
            HeaderLength  = 4, // "NAME"   .Length
            DefaultLength = 7; // "default".Length

        return Math.Max(
            HeaderLength,
            contextSets.Max(s => s.Contexts.Max(c => c.DatabaseName?.Length ?? DefaultLength))
        );
    }

    private static int ComputeMigrationNameColumnWidth(ImmutableArray<Migration> migrations)
    {
        const int
            HeaderLength  = 4; // "NAME".Length

        return Math.Max(HeaderLength, migrations.Max(m => m.Name.Length));
    }

    private void ReportStarting(MigrationTarget target)
    {
        Console.WriteHost(string.Format(
            @"[+{0:hh\:mm\:ss}] {1} {2}:{3} Starting",
            /*{0}*/ _totalTime.Elapsed,
            /*{1}*/ Phase.ToFixedWidthString(),
            /*{2}*/ target.DatabaseName,
            /*{3}*/ Space.Pad(target.DatabaseName, _databaseNameColumnWidth)
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
        // NAME             FILES     PROGRESS          DEPENDS-ON
        // 2023-01-01-123   Ok        (new)             (none)
        // 2023-01-02-234   Missing   Pre->Core->Post   (none)
        // 2023-01-03-345   Ok        Pre->Core         2023-01-01-123
        // 2023-01-04-456   Changed   Pre               (none)

        target.Log($"Migrations:         {migrations.Length}");
        target.Log("");

        target.Log(string.Format(
            "NAME{0}   FILES     PROGRESS          DEPENDS-ON",
            Space.Pad("NAME", _migrationNameColumnWidth)
        ));

        foreach (var migration in migrations)
        {
            if (migration.IsPseudo)
                continue;

            target.Log(string.Format(
                "{0}{1}   {2}   {3}   {4}",
                /*{0}*/ migration.Name,
                /*{1}*/ Space.Pad(migration.Name, _migrationNameColumnWidth),
                /*{2}*/ migration.GetFixedWithFileStatusString(),
                /*{3}*/ migration.State.ToFixedWidthString(),
                /*{4}*/ migration.Depends?.LastOrDefault() ?? "(none)"
            ));
        }

        target.Log("");
    }

    private void ReportPlan(MigrationPlan plan, MigrationTarget target)
    {
        target.Log("Sequence:");
        target.Log("");

        var items = plan.GetItems(Phase);

        target.Log(string.Format(
            "NAME{0}   PHASE",
            Space.Pad("NAME", _migrationNameColumnWidth)
        ));

        foreach (var (migration, phase) in items)
            target.Log(string.Format(
                "{0}{1}   {2}",
                /*{0}*/ migration.Name,
                /*{1}*/ Space.Pad(migration.Name, _migrationNameColumnWidth),
                /*{2}*/ phase
            ));

        target.Log("");
    }

    private void ReportApplying(Migration migration, MigrationPhase phase, MigrationTarget target)
    {
        Console.WriteHost(string.Format(
            @"[+{0:hh\:mm\:ss}] {1} {2}:{3} Applying {4} {5}",
            /*{0}*/ _totalTime.Elapsed,
            /*{1}*/ Phase.ToFixedWidthString(),
            /*{2}*/ target.DatabaseName,
            /*{3}*/ Space.Pad(target.DatabaseName, _databaseNameColumnWidth),
            /*{4}*/ migration.Name,
            /*{5}*/ phase
        ));
    }

    private void ReportApplied(int count, MigrationTarget target, Exception? exception)
    {
        var abnormality = 
            exception is not null ? " [EXCEPTION]"  :
            _errorCount > 0       ? " [INCOMPLETE]" :
            null;

        Console.WriteHost(string.Format(
            @"[+{0:hh\:mm\:ss}] {1} {2}:{3} Applied {4} migration(s) in {5:N3} second(s){6}",
            /*{0}*/ _totalTime.Elapsed,
            /*{1}*/ Phase.ToFixedWidthString(),
            /*{2}*/ target.DatabaseName,
            /*{3}*/ Space.Pad(target.DatabaseName, _databaseNameColumnWidth),
            /*{4}*/ count,
            /*{5}*/ target.ElapsedTime.TotalSeconds,
            /*{6}*/ abnormality
        ));
    }
}
