// Copyright 2023 Subatomix Research Inc.
// SPDX-License-Identifier: ISC

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
    }

    /// <summary>
    ///   Gets the defined migrations.  The default value is an empty array.
    /// </summary>
    /// <remarks>
    ///   Invoke <see cref="DiscoverMigrations"/> to populate this property.
    /// </remarks>
    public ImmutableArray<Migration> Migrations { get; private set; }

    /// <summary>
    ///   Gets the minimum (earliest) defined migration name, excluding the
    ///   <c>_Begin</c> and <c>_End</c> pseudo-migrations.  The default value
    ///   is the empty string.
    /// </summary>
    /// <remarks>
    ///   Invoke <see cref="DiscoverMigrations"/> to populate this property.
    /// </remarks>
    public string MinimumMigrationName { get; private set; }

    /// <summary>
    ///   Gets the context sets specifying the target databases.  The default
    ///   value is an empty array.
    /// </summary>
    /// <remarks>
    ///   Invoke <see cref="SpecifyTargets"/> to populate this property.
    /// </remarks>
    public ImmutableArray<SqlContextParallelSet> Targets { get; private set; }

    /// <summary>
    ///   Gets the phase in which this instance most recently applied
    ///   migrations.  The default value is <see cref="MigrationPhase.Pre"/>.
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
    //private int _errorCount;

    // For report tabulation
    private int _databaseNameColumnWidth;

    /// <summary>
    ///   Discovers defined migrations in the specified directory path.
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
    ///   Specifies the target databases.
    /// </summary>
    /// <param name="contextSets">
    ///   The context sets specifying the target databases.
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
    ///   Applies any outstanding migrations for the specified phase to target
    ///   databases asynchronously.
    /// </summary>
    /// <param name="phase">
    ///   The phase in which migrations are being applied.
    /// </param>
    /// <returns>
    ///   A <see cref="Task"/> representing the asynchronous operation.
    /// </returns>
    public Task ApplyAsync(MigrationPhase phase)
    {
        Phase = phase;

        if (Targets.Length == 0)
            return Task.CompletedTask;

        Directory.CreateDirectory(LogPath);

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

        async Task RunLimitedAsync(SqlContext context)
        {
            await limiter.WaitAsync(CancellationToken);
            try
            {
                // Move to another thread so sibling task can start next context
                await Task.Yield();
                using var target = new MigrationTarget(this, context);
                await target.ApplyAsync();
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

    internal void ReportStarting(string databaseName)
    {
        Console.WriteHost(string.Format(
            @"[+{0:hh\:mm\:ss}] {1} {2}:{3} Starting",
            /*{0}*/ _totalTime.Elapsed,
            /*{1}*/ Phase.ToFixedWidthString(),
            /*{2}*/ databaseName,
            /*{3}*/ Space.Pad(databaseName, _databaseNameColumnWidth)
        ));
    }

    internal void ReportApplying(
        string         databaseName,
        string         migrationName,
        MigrationPhase phase)
    {
        Console.WriteHost(string.Format(
            @"[+{0:hh\:mm\:ss}] {1} {2}:{3} Applying {4} {5}",
            /*{0}*/ _totalTime.Elapsed,
            /*{1}*/ Phase.ToFixedWidthString(),
            /*{2}*/ databaseName,
            /*{3}*/ Space.Pad(databaseName, _databaseNameColumnWidth),
            /*{4}*/ migrationName,
            /*{5}*/ phase
        ));
    }

    internal void ReportApplied(
        string     databaseName,
        int        count,
        TimeSpan   elapsed,
        Exception? exception)
    {
        var abnormality = 
            exception is not null ? " [EXCEPTION]"  :
            //_errorCount > 0       ? " [INCOMPLETE]" :
            null;

        Console.WriteHost(string.Format(
            @"[+{0:hh\:mm\:ss}] {1} {2}:{3} Applied {4} migration(s) in {5:N3} second(s){6}",
            /*{0}*/ _totalTime.Elapsed,
            /*{1}*/ Phase.ToFixedWidthString(),
            /*{2}*/ databaseName,
            /*{3}*/ Space.Pad(databaseName, _databaseNameColumnWidth),
            /*{4}*/ count,
            /*{5}*/ elapsed.TotalSeconds,
            /*{6}*/ abnormality
        ));
    }
}
