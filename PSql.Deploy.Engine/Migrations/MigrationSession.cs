// Copyright Subatomix Research Inc.
// SPDX-License-Identifier: MIT

using System.Collections.Concurrent;
using System.Numerics;

namespace PSql.Deploy.Migrations;

using static BitOperations;

/// <summary>
///   A session in which schema migrations are applied to target databases.
/// </summary>
public class MigrationSession : IMigrationSessionControl, IMigrationSessionInternal, IDisposable
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
            _targetSets = new List<TargetSet>();

        _cancellation = new CancellationTokenSource();
        _tasks        = new();
    }

    /// <summary>
    ///   Gets the options for the session.
    /// </summary>
    public MigrationSessionOptions Options { get; }

    /// <inheritdoc/>
    public IMigrationConsole Console { get; }

    private readonly ICollection<TargetSet>? _targetSets;
    private readonly CancellationTokenSource _cancellation;
    private readonly ConcurrentBag<Task>     _tasks;

    /// <inheritdoc/>
    public CancellationToken CancellationToken
        => _cancellation.Token;

    /// <inheritdoc/>
    public bool IsEnabled(MigrationPhase phase)
        => ((int) Options & 1 << (int) phase) is not 0;

    /// <inheritdoc/>
    public bool AllowContentInCorePhase
        => (Options & MigrationSessionOptions.AllowContentInCorePhase) is not 0;

    /// <inheritdoc/>
    public bool IsWhatIfMode
        => (Options & MigrationSessionOptions.IsWhatIfMode) is not 0;

    /// <inheritdoc/>
    public MigrationPhase CurrentPhase { get; private set; }

    /// <inheritdoc/>
    public ImmutableArray<IMigration> Migrations
        => MigrationsInternal.CastArray<IMigration>();

    /// <inheritdoc/>
    internal ImmutableArray<Migration> MigrationsInternal { get; private set; }

    /// <inheritdoc/>
    public string EarliestDefinedMigrationName { get; private set; } = "";

    /// <inheritdoc/>
    public void DiscoverMigrations(string path, string? latestName = null)
    {
        MigrationsInternal           = MigrationRepository.GetAll(path, latestName);
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
    void IMigrationSessionInternal.LoadContent(Migration migration)
        => MigrationLoader.LoadContent(migration);

    public void BeginApplying(TargetSet targetSet)
    {
    }

    public void BeginApplying(Target target)
    {
        var s = new MigrationApplicator(this, target);

        _tasks.Add(s.ApplyAsync());
    }

    public async Task CompleteApplyingAsync(CancellationToken cancellation = default)
    {
        using var _ = cancellation.Register(_cancellation.Cancel, useSynchronizationContext: false);

        await Task.WhenAll(_tasks).ConfigureAwait(continueOnCapturedContext: false);
    }

    public virtual void Dispose()
    {
        _cancellation.Dispose();
    }

    private static MigrationPhase GetMinPhase(MigrationSessionOptions options)
        => (MigrationPhase) TrailingZeroCount((int) options);

    private static int GetPhaseCount(MigrationSessionOptions options)
        => PopCount((uint) (options & MigrationSessionOptions.AllPhases));
}
