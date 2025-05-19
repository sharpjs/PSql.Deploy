// Copyright Subatomix Research Inc.
// SPDX-License-Identifier: MIT

using System.Collections.Concurrent;
using System.Numerics;

namespace PSql.Deploy.Migrations;

using static BitOperations;

/// <summary>
///   A session in which schema migrations are applied to target databases.
/// </summary>
public class MigrationSession : DeploymentSession, IMigrationSessionInternal
{
    /// <summary>
    ///   Initializes a new <see cref="MigrationSession"/> instance with the
    ///   specified options and console.
    /// </summary>
    /// <param name="options">
    ///   The options for the session.  If the options specify no phases, the
    ///   session enables <see cref="MigrationSessionOptions.AllPhases"/>.
    /// </param>
    /// <param name="console">
    ///   The user interface to report the progress of the session.
    /// </param>
    /// <exception cref="ArgumentNullException">
    ///   <paramref name="console"/> is <see langword="null"/>.
    /// </exception>
    public MigrationSession(MigrationSessionOptions options, IMigrationConsole console)
    {
        if (console is null)
            throw new ArgumentNullException(nameof(console));

        var phaseCount = GetPhaseCount(options);
        if (phaseCount is 0)
            options |= MigrationSessionOptions.AllPhases;

        Options      = options;
        Console      = console;
        CurrentPhase = GetMinPhase(options);
        _isNextPhase = false;

        if (phaseCount is not 1)
            _targetGroups = [];

        if (IsWhatIfMode)
            _whatIfState = new();
    }

    /// <summary>
    ///   Gets the options for the session.
    /// </summary>
    public MigrationSessionOptions Options { get; }

    /// <inheritdoc/>
    public IMigrationConsole Console { get; }

    /// <inheritdoc/>
    public bool IsEnabled(MigrationPhase phase)
        => ((int) Options & 1 << (int) phase) is not 0;

    /// <inheritdoc/>
    public bool AllowContentInCorePhase
        => (Options & MigrationSessionOptions.AllowContentInCorePhase) is not 0;

    /// <inheritdoc/>
    [MemberNotNullWhen(true, nameof(_whatIfState))]
    public override bool IsWhatIfMode
        => (Options & MigrationSessionOptions.IsWhatIfMode) is not 0;

    /// <inheritdoc/>
    public MigrationPhase CurrentPhase { get; private set; }

    /// <inheritdoc/>
    public ImmutableArray<Migration> Migrations { get; private set; }

    /// <inheritdoc/>
    public string EarliestDefinedMigrationName { get; private set; } = "";

    /// <summary>
    ///   Gets or sets the factory for connections to target databases.
    /// </summary>
    /// <remarks>
    ///   This property enables tests to inject a mock connection factory.
    /// </remarks>
    internal MigrationTargetConnectionFactory ConnectionFactory { get; set; }
        = (target, logger) => new SqlMigrationTargetConnection(target, logger);

    // What has been simulated in what-if mode
    private readonly WhatIfMigrationState? _whatIfState;

    // Targets/groups to repeat in a multi-phase session; null for single-phase
    private readonly ConcurrentQueue<TargetGroup>? _targetGroups;

    // Whether advanced beyond initial phase in a multi-phase session
    private bool _isNextPhase;

    /// <inheritdoc/>
    public void DiscoverMigrations(string path, string? latestName = null)
    {
        Migrations                   = MigrationRepository.GetAll(path, latestName);
        EarliestDefinedMigrationName = Migrations.FirstOrDefault(m => !m.IsPseudo)?.Name ?? "";
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<Migration>> GetRegisteredMigrationsAsync(
        Target target, string? earliestName = null, ISqlMessageLogger? logger = null)
    {
        await using var connection = Connect(target, logger ?? NullSqlMessageLogger.Instance);

        await connection.OpenAsync(CancellationToken);

        return await connection.GetAppliedMigrationsAsync(earliestName, CancellationToken);
    }

    /// <inheritdoc/>
    public override void BeginApplying(TargetGroup group)
    {
        base.BeginApplying(group);

        // If in the initial phase of a multi-phase session, remember the
        // target group for later re-application in subsequent phases.
        if (_targetGroups is { } queue && !_isNextPhase)
            queue.Enqueue(group);
    }

    /// <inheritdoc/>
    public override async Task CompleteApplyingAsync(CancellationToken cancellation = default)
    {
        for (;;)
        {
            // Wait for phase to complete
            await base.CompleteApplyingAsync(cancellation);

            // Check if multi-phase session
            if (_targetGroups is not { } queue)
                return;

            // Advance to next phase
            if (!AdvanceToNextPhase())
                break;

            // Run next phase
            foreach (var group in queue)
                BeginApplying(group);
        }
    }

    private bool AdvanceToNextPhase()
    {
        var phase = CurrentPhase;

        while (++phase <= MigrationPhase.Post)
        {
            if (!IsEnabled(phase))
                continue;

            Thread.MemoryBarrier();
            CurrentPhase = phase;
            _isNextPhase = true;
            Thread.MemoryBarrier();
            return true;
        }

        return false;
    }

    /// <inheritdoc/>
    protected override Task ApplyCoreAsync(Target target, int maxParallelism)
    {
        return new MigrationApplicator(this, target).ApplyAsync();
    }

    private IMigrationTargetConnection Connect(Target target, ISqlMessageLogger logger)
    {
        var connection = ConnectionFactory.Invoke(target, logger);

        if (IsWhatIfMode)
            connection = new WhatIfMigrationTargetConnection(connection, _whatIfState);

        return connection;
    }

    /// <inheritdoc/>
    IMigrationTargetConnection IMigrationSessionInternal
        .Connect(Target target, ISqlMessageLogger logger)
        => Connect(target, logger);

    /// <inheritdoc/>
    void IMigrationSessionInternal.LoadContent(Migration migration)
        => MigrationLoader.LoadContent(migration);

    private static MigrationPhase GetMinPhase(MigrationSessionOptions options)
        => (MigrationPhase) TrailingZeroCount((int) options);

    private static int GetPhaseCount(MigrationSessionOptions options)
        => PopCount((uint) (options & MigrationSessionOptions.AllPhases));

    /// <inheritdoc/>
    protected override Exception Transform(Exception exception)
        => exception as MigrationException
        ?? new MigrationException(message: null, exception);
}
