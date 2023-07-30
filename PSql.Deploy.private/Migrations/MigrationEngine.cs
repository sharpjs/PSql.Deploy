// Copyright 2023 Subatomix Research Inc.
// SPDX-License-Identifier: ISC

using System.Diagnostics;

namespace PSql.Deploy.Migrations;

/// <inheritdoc cref="IMigrationEngine"/>
internal class MigrationEngine : IMigrationSession, IMigrationEngine
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

    /// <inheritdoc/>
    public CancellationToken CancellationToken { get; }

    /// <inheritdoc/>
    public bool HasErrors => Volatile.Read(ref _errorCount) > 0;

    /// <inheritdoc/>
    public bool AllowCorePhase { get; set; }

    /// <inheritdoc/>
    public bool IsWhatIfMode { get; set; }

    // Time elapsed since construction
    private readonly Stopwatch _totalTime;

    // Count of applications to target databases that threw exceptions
    private int _errorCount;

    // For report tabulation
    private int _databaseNameColumnWidth;

    /// <inheritdoc/>
    public void DiscoverMigrations(string path, string? maxName = null)
    {
        Migrations           = MigrationRepository.GetAll(path, maxName);
        MinimumMigrationName = Migrations.FirstOrDefault(m => !m.IsPseudo)?.Name ?? "";
    }

    /// <inheritdoc/>
    public void SpecifyTargets(IEnumerable<SqlContextParallelSet> contextSets)
    {
        if (contextSets is null)
            throw new ArgumentNullException(nameof(contextSets));

        Targets                  = contextSets.ToImmutableArray();
        _databaseNameColumnWidth = ComputeDatabaseNameColumnWidth(Targets);
    }

    /// <inheritdoc/>
    public async Task ApplyAsync(MigrationPhase phase)
    {
        Phase = phase;

        if (Targets.Length == 0)
            return;

        Directory.CreateDirectory(LogPath);

        await Task.WhenAll(Targets.Select(ApplyAsync));

        if (HasErrors)
            throw new MigrationException();
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
                await ApplyAsync(context);
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
        using var target = new MigrationTarget(this, context)
        {
            AllowCorePhase = AllowCorePhase,
            IsWhatIfMode   = IsWhatIfMode,
        };

        try
        {
            await target.ApplyAsync();
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch
        {
            Interlocked.Increment(ref _errorCount);
        }
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

    Task<IReadOnlyList<Migration>> IMigrationSession
        .GetAppliedMigrationsAsync(SqlContext context, IConsole console)
    {
        return MigrationRepository.GetAllAsync(
            context, MinimumMigrationName,
            console, CancellationToken
        );
    }

    /// <inheritdoc/>
    TextWriter IMigrationSession.CreateLog(string fileName)
        => new StreamWriter(Path.Combine(LogPath, fileName));

    /// <inheritdoc/>
    void IMigrationSession.ReportStarting(string databaseName)
    {
        Console.WriteHost(string.Format(
            @"[+{0:hh\:mm\:ss}] {1} {2}:{3} Starting",
            /*{0}*/ _totalTime.Elapsed,
            /*{1}*/ Phase.ToFixedWidthString(),
            /*{2}*/ databaseName,
            /*{3}*/ Space.Pad(databaseName, _databaseNameColumnWidth)
        ));
    }

    /// <inheritdoc/>
    void IMigrationSession.ReportApplying(
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

    /// <inheritdoc/>
    void IMigrationSession.ReportApplied(
        string                     databaseName,
        int                        count,
        TimeSpan                   elapsed,
        MigrationTargetDisposition disposition)
    {
        Console.WriteHost(string.Format(
            @"[+{0:hh\:mm\:ss}] {1} {2}:{3} Applied {4} migration(s) in {5:N3} second(s){6}",
            /*{0}*/ _totalTime.Elapsed,
            /*{1}*/ Phase.ToFixedWidthString(),
            /*{2}*/ databaseName,
            /*{3}*/ Space.Pad(databaseName, _databaseNameColumnWidth),
            /*{4}*/ count,
            /*{5}*/ elapsed.TotalSeconds,
            /*{6}*/ disposition.ToMarker()
        ));
    }

    /// <inheritdoc/>
    void IMigrationSession.ReportProblem(string message)
    {
        Console.WriteWarning(message);
    }
}
