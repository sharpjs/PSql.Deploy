// Copyright 2023 Subatomix Research Inc.
// SPDX-License-Identifier: ISC

using System.Collections.Immutable;
using System.Diagnostics;

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
    ///   The console on which to display status and messages.
    /// </param>
    /// <param name="cancellation">
    ///   The token to monitor for cancellation requests.
    /// </param>
    /// <exception cref="ArgumentNullException">
    ///   <paramref name="console"/> is <see langword="null"/>.
    /// </exception>
    public MigrationEngine(IConsole console, string logPath, CancellationToken cancellation)
    {
        if (console is null)
            throw new ArgumentNullException(nameof(console));
        if (logPath is null)
            throw new ArgumentNullException(nameof(logPath));

        Migrations           = ImmutableArray<Migration>.Empty;
        MinimumMigrationName = "";
        Console              = console;
        LogPath              = logPath;
        CancellationToken    = cancellation;
        _totalStopwatch      = new();
    }

    /// <summary>
    ///   Gets the migrations to be applied to targets.
    /// </summary>
    public ImmutableArray<Migration> Migrations { get; private set; }

    /// <summary>
    ///   Gets the minimum name of the migrations to be applied to targets, or
    ///   string if <see cref="Migrations"/> is empty.
    /// </summary>
    public string MinimumMigrationName { get; private set; }

    /// <summary>
    ///   Gets or sets the phase of the migrations to be applied to targets.
    /// </summary>
    public MigrationPhase Phase { get; set; }

    /// <summary>
    ///   Gets the console on which to display status and messages.
    /// </summary>
    public IConsole Console { get; }

    /// <summary>
    ///   Gets the path to the directory for per-database log files.
    /// </summary>
    public string LogPath { get; }

    /// <summary>
    ///   Gets the token to monitor for cancellation requests.
    /// </summary>
    public CancellationToken CancellationToken { get; }

    // Measures total elapsed time of migration run
    private readonly Stopwatch _totalStopwatch;

    // Whether any thread encountered an error
    private int _errorCount;

    /// <summary>
    ///   Discovers migrations in the specified path.
    /// </summary>
    /// <param name="path">
    ///   The path in which to discover migrations.
    /// </param>
    public void AddMigrationsFromPath(string path)
    {
        Migrations           = LocalMigrationDiscovery.GetLocalMigrations(path);
        MinimumMigrationName = GetMinimumMigrationName() ?? "";
    }

    /// <summary>
    ///   Applies migrations to the specified targets asynchronously.
    /// </summary>
    /// <param name="targets">
    ///   The targets to which to apply migrations.
    /// </param>
    /// <returns>
    ///   A <see cref="Task"/> representing the asynchronous operation.
    /// </returns>
    public async Task RunAsync(IReadOnlyList<SqlContextParallelSet> targets)
    {
        if (targets is null)
            throw new ArgumentNullException(nameof(targets));

        _totalStopwatch.Start();

        Directory.CreateDirectory(LogPath);

        await Task.WhenAll(targets.Select(RunAsync));
    }

    private async Task RunAsync(SqlContextParallelSet targets)
    {
        if (targets is null)
            throw new ArgumentNullException(nameof(targets));

        await Task.Yield(); // Parallelize

        using var limiter = new SemaphoreSlim(targets.Parallelism, targets.Parallelism);

        async Task RunLimitedAsync(SqlContext target)
        {
            await limiter.WaitAsync(CancellationToken);
            try
            {
                await RunAsync(target);
            }
            finally
            {
                limiter.Release();
            }
        }

        await Task.WhenAll(targets.Contexts.Select(RunLimitedAsync));
    }

    private async Task RunAsync(SqlContext target)
    {
        if (target is null)
            throw new ArgumentNullException(nameof(target));

        await Task.Yield(); // Parallelize

        using var log = OpenLog(target);

        ReportTarget(target, log);

        // Discover migrations on target
        var migrations = await RemoteMigrationDiscovery.GetServerMigrationsAsync(
            target, MinimumMigrationName, log, CancellationToken
        );

        // Plan what to do
        var plan = CreatePlan(migrations, target, log);
        if (plan is null)
            return;

        // Run the plan
        await RunCoreAsync(plan, target, log);
    }

    private async Task RunCoreAsync(MigrationPlan plan, SqlContext target, FileConsole log)
    {
        var items = plan.GetItems(Phase);
        if (!items.Any())
            return;

        var stopwatch  = Stopwatch.StartNew();
        var count      = 0;
        var exception  = null as Exception;

        using var connection = target.Connect(null, log);
        using var command    = connection.CreateCommand();

        try
        {
            foreach (var (migration, phase) in items)
            {
                // Stop if another thread encountered an error
                if (_errorCount > 0)
                    return;

                ReportApplying(migration, phase, target);
                connection.ClearErrors();

                await RunCoreAsync(migration, phase, command);

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
            stopwatch.Stop();
            ReportApplied(count, target, stopwatch.Elapsed, exception);
        }
    }

    private Task RunCoreAsync(Migration migration, MigrationPhase phase, ISqlCommand command)
    {
        var sql = migration.GetSql(phase);
        if (sql.IsNullOrEmpty())
            return Task.CompletedTask;
 
        command.CommandText    = sql;
        command.CommandTimeout = 0; // No timeout

        return command.UnderlyingCommand.ExecuteNonQueryAsync(CancellationToken);
    }

    private string? GetMinimumMigrationName()
    {
        foreach (var migration in Migrations)
            if (!migration.IsPseudo)
                return migration.Name;

        return null;
    }

    private FileConsole OpenLog(SqlContext target)
    {
        var serverName   = target.AsAzure?.ServerResourceName ?? target.ServerName ?? "local";
        var databaseName = target.DatabaseName ?? "default";
        var fileName     = $"{serverName}.{databaseName}.{Phase}.log".SanitizeFileName();

        return new(Path.Combine(LogPath, fileName));
    }

    private MigrationPlan? CreatePlan(IReadOnlyList<Migration> migrations, SqlContext target, FileConsole log)
    {
        var merged = new MigrationMerger(Phase).Merge(Migrations, migrations);

        if (!HasOutstandingMigrations(merged))
            return OnNothingToDo(target, log);

        ReportMigrations(merged, log);

        if (!new MigrationValidator(target, Phase, Console).Validate(merged, target))
            return null;

        return new MigrationPlanner(merged).CreatePlan();
    }

    private bool HasOutstandingMigrations(ReadOnlySpan<Migration> migrations)
    {
        foreach (var migration in migrations)
            if (!migration.IsPseudo)
                return true;

        return false;
    }

    private MigrationPlan? OnNothingToDo(SqlContext target, FileConsole console)
    {
        ReportApplied(0, target,  default, null);
        ReportNoMigrations(console);
        return null;
    }

    private void ReportTarget(SqlContext target, IConsole console)
    {
        console.WriteVerbose("PSql.Deploy Migration Log");
        console.WriteVerbose("");
        console.WriteVerbose("Target Server:   " + target.GetEffectiveServerName());
        console.WriteVerbose("Target Database: " + target.DatabaseName);
        console.WriteVerbose("Migration Phase: " + Phase);
        console.WriteVerbose("Start Time:      " + DateTime.UtcNow.ToString("o"));
        console.WriteVerbose("Machine:         " + Environment.MachineName);
        console.WriteVerbose("User:            " + Environment.UserName);
        console.WriteVerbose("Process ID:      " + Process.GetCurrentProcess().Id);
    }

    private void ReportNoMigrations(FileConsole console)
    {
        console.WriteVerbose($"Migrations:      0");
        console.WriteVerbose("");
        console.WriteVerbose("Nothing to do.");
    }

    private void ReportMigrations(ReadOnlySpan<Migration> migrations, FileConsole console)
    {
        console.WriteVerbose($"Migrations:      {migrations.Length}");
        console.WriteVerbose("");

        var nameColumnWidth = 4;

        foreach (var migration in migrations)
            if (!migration.IsPseudo)
                nameColumnWidth = Math.Max(nameColumnWidth, migration.Name!.Length);

        // NAME               FILES     PROGRESS          DEPENDS-ON
        // 2023-01-01-10042   Ok        (new)             (none)
        // 2023-01-02-10128   Missing   Pre->Core->Post   (none)
        // 2023-01-03-10420   Ok        Pre->Core         2023-01-01-10042
        // 2023-01-04-10777   Changed   Pre               (none)

        console.WriteHost(string.Format(
            "NAME{0}   FILES     PROGRESS          DEPENDS-ON",
            new string(' ', nameColumnWidth - 4)
        ));

        foreach (var migration in migrations)
            if (!migration.IsPseudo)
                console.WriteHost(string.Format(
                    "{0}{1}   {2}   {3}   {4}",
                    migration.Name,
                    new string(' ', nameColumnWidth - migration.Name!.Length),
                    migration.HasChanged switch
                    {
                        true                                => "Changed",
                        false when migration.PreSql is null => "Missing",
                        _                                   => "Ok     ",
                    },
                    migration.State2 switch
                    {
                        MigrationState.NotApplied  => "(new)          ",
                        MigrationState.AppliedPre  => "Pre            ",
                        MigrationState.AppliedCore => "Pre->Core      ",
                        _                          => "Pre->Core->Post",
                    },
                    migration.Depends?.LastOrDefault() ?? "(none)"
                ));
        console.WriteVerbose("");
    }

    private void ReportApplying(Migration migration, MigrationPhase phase, SqlContext target)
    {
        Console.WriteHost(string.Format(
            @"[+{0:hh\:mm\:ss}] {1}: Applying {2} {3}",
            _totalStopwatch.Elapsed,
            target.DatabaseName,
            migration.Name,
            phase
        ));
    }

    private void ReportApplied(int count, SqlContext target, TimeSpan elapsed, Exception? exception)
    {
        var abnormality = 
            exception is not null ? " [EXCEPTION]"  :
            _errorCount > 0       ? " [INCOMPLETE]" :
            null;

        Console.WriteHost(string.Format(
            @"[+{0:hh\:mm\:ss}] {1}: Applied {2} {3} item(s) in {4:N3} second(s){5}",
            _totalStopwatch.Elapsed,
            target.DatabaseName,
            count,
            Phase,
            elapsed.TotalSeconds,
            abnormality
        ));
    }
}
