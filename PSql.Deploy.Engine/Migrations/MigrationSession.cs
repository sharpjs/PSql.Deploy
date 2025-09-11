// Copyright Subatomix Research Inc.
// SPDX-License-Identifier: MIT

using System.Collections.Concurrent;

namespace PSql.Deploy.Migrations;

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
    ///   The options for the session.
    /// </param>
    /// <param name="console">
    ///   The user interface to report the progress of the session.
    /// </param>
    /// <exception cref="ArgumentNullException">
    ///   <paramref name="options"/> and/or
    ///   <paramref name="console"/> is <see langword="null"/>.
    /// </exception>
    /// <exception cref="ArgumentException">
    ///    <paramref name="options"/> specifies that no phases are enabled.
    /// </exception>
    public MigrationSession(MigrationSessionOptions options, IMigrationConsole console)
        : base(options)
    {
        // options null-checked by base constructor
        ArgumentNullException.ThrowIfNull(console);

        Console                 = console;
        EnabledPhases           = new(options.EnabledPhases);
        AllowContentInCorePhase = options.AllowContentInCorePhase;

        // At least one phase must be enabled
        if (EnabledPhases.Count is 0)
            throw OnNoEnabledPhases();

        // Multi-phase sessions need to remember target groups for later phases
        if (EnabledPhases.Count > 1)
            _targetGroups = [];

        // What-if sessions need to remember what they have pretended to do
        if (IsWhatIfMode)
            _whatIfState = new();

        CurrentPhase = EnabledPhases.First();
        _isNextPhase = false;
    }

    /// <inheritdoc/>
    public IMigrationConsole Console { get; }

    /// <inheritdoc/>
    public MigrationPhaseSet EnabledPhases { get; }

    /// <inheritdoc/>
    public MigrationPhase CurrentPhase { get; private set; }

    /// <inheritdoc/>
    public bool AllowContentInCorePhase { get; }

    /// <inheritdoc/>
    [MemberNotNullWhen(true, nameof(_whatIfState))]
    public new bool IsWhatIfMode => base.IsWhatIfMode;

    /// <inheritdoc/>
    public ImmutableArray<Migration> Migrations { get; private set; }
        = ImmutableArray<Migration>.Empty;

    /// <inheritdoc/>
    public string EarliestDefinedMigrationName { get; private set; }
        = "";

    /// <summary>
    ///   Gets or sets the factory for connections to target databases.
    /// </summary>
    /// <remarks>
    ///   This property enables tests to inject a mock connection factory.
    /// </remarks>
    internal MigrationTargetConnectionFactory ConnectionFactory { get; set; }
        = (target, logger) => new SqlMigrationTargetConnection(target, logger);

    // Targets/groups to repeat in a multi-phase session; null for single-phase
    private readonly ConcurrentQueue<TargetGroup>? _targetGroups;

    // What has been simulated in what-if mode
    private readonly WhatIfMigrationState? _whatIfState;

    // Whether the session has advanced beyond the initial phase (and is multi-phase)
    private bool _isNextPhase;

    /// <inheritdoc/>
    public void DiscoverMigrations(string path, string? latestName = null)
    {
        Migrations                   = MigrationDiscoverer.GetAll(path, latestName);
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
            if (!EnabledPhases.Contains(phase))
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
    protected override int GetMaxParallelTargets(TargetGroup group)
    {
        // Migrations do not use per-target parallelism
        return group.MaxParallelism;
    }

    /// <inheritdoc/>
    protected override Task ApplyCoreAsync(Target target, TargetParallelism _)
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

    /// <inheritdoc/>
    protected override Exception Transform(Exception exception)
    {
        return exception as MigrationException
            ?? new MigrationException(message: null, exception);
    }

    private static ArgumentException OnNoEnabledPhases()
    {
        return new ArgumentException("At least one migration phase must be enabled.", "options");
    }
}
