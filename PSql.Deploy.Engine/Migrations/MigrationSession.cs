// Copyright Subatomix Research Inc.
// SPDX-License-Identifier: MIT

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

        if (phaseCount is not 1)
            _targetGroups = [];
    }

    /// <summary>
    ///   Gets the options for the session.
    /// </summary>
    public MigrationSessionOptions Options { get; }

    /// <inheritdoc/>
    public IMigrationConsole Console { get; }

    private readonly ICollection<TargetGroup>? _targetGroups;

    /// <inheritdoc/>
    public bool IsEnabled(MigrationPhase phase)
        => ((int) Options & 1 << (int) phase) is not 0;

    /// <inheritdoc/>
    public bool AllowContentInCorePhase
        => (Options & MigrationSessionOptions.AllowContentInCorePhase) is not 0;

    /// <inheritdoc/>
    public override bool IsWhatIfMode
        => (Options & MigrationSessionOptions.IsWhatIfMode) is not 0;

    /// <inheritdoc/>
    public MigrationPhase CurrentPhase { get; private set; }

    /// <inheritdoc/>
    public ImmutableArray<Migration> Migrations { get; private set; }

    /// <inheritdoc/>
    public string EarliestDefinedMigrationName { get; private set; } = "";

    /// <inheritdoc/>
    public void DiscoverMigrations(string path, string? latestName = null)
    {
        Migrations                   = MigrationRepository.GetAll(path, latestName);
        EarliestDefinedMigrationName = Migrations.FirstOrDefault(m => !m.IsPseudo)?.Name ?? "";
    }

    Task<IReadOnlyList<Migration>> IMigrationSessionInternal.GetAppliedMigrationsAsync(Target target)
    {
        return MigrationRepository.GetAllAsync(
            target, EarliestDefinedMigrationName,
            logger: null!/*TODO*/, CancellationToken
        );
    }

    /// <inheritdoc/>
    protected override Task ApplyCoreAsync(Target target, int maxParallelism)
    {
        return new MigrationApplicator(this, target).ApplyAsync();
    }

    /// <inheritdoc/>
    void IMigrationSessionInternal.LoadContent(Migration migration)
        => MigrationLoader.LoadContent(migration);

    private static MigrationPhase GetMinPhase(MigrationSessionOptions options)
        => (MigrationPhase) TrailingZeroCount((int) options);

    private static int GetPhaseCount(MigrationSessionOptions options)
        => PopCount((uint) (options & MigrationSessionOptions.AllPhases));

    /// <inheritdoc/>
    protected override Exception Transform(Exception exception)
    {
        return exception as MigrationException
            ?? new MigrationException(message: null, exception);
    }
}
