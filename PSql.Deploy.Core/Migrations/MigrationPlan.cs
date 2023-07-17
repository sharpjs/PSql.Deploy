// Copyright 2023 Subatomix Research Inc.
// SPDX-License-Identifier: ISC

namespace PSql.Deploy.Migrations;

/// <summary>
///   A plan for the order in which to run migrations.
/// </summary>
internal class MigrationPlan
{
    /// <summary>
    ///   Initializes a new <see cref="MigrationPlan"/> instance for the
    ///   specified migrations.
    /// </summary>
    /// <param name="pendingMigrations">
    ///   The pending migrations for which the plan was computed.
    /// </param>
    public MigrationPlan(ImmutableArray<Migration> pendingMigrations)
    {
        PendingMigrations = pendingMigrations;
    }

    /// <summary>
    ///   Gets the pending migrations for which the plan was computed.
    /// </summary>
    public ImmutableArray<Migration> PendingMigrations { get; }

    /// <summary>
    ///   Gets the migrations whose <c>Pre</c> components actually will run
    ///   during the <c>Pre</c> phase.
    /// </summary>
    public List<Migration> Pre { get; } = new();

    /// <summary>
    ///   Gets the migrations to run during the <c>Core</c> phase.
    /// </summary>
    public List<(Migration Migration, MigrationPhase Phase)> Core { get; } = new();

    /// <summary>
    ///   Gets the migrations whose <c>Post</c> components actually will run
    ///   during the <c>Post</c> phase.
    /// </summary>
    public List<Migration> Post { get; } = new();

    /// <summary>
    ///   Gets whether the <c>Core</c> phase is required.
    /// </summary>
    /// <remarks>
    ///   This property exists to support zero-downtime deployment scenarios.
    ///   In such scenarios, the <c>Core</c> phase is for any migration scripts
    ///   that require downtime and thus break zero-downtime guarantees.  This
    ///   property detects when a migration has content that must run in the
    ///   <c>Core</c> phase, whether directly via inclusion of the content or
    ///   indirectly as required to satisfy an inter-migration dependency.
    /// </remarks>
    public bool IsCoreRequired => Core.FindIndex(IsRequired) >= 0;

    /// <summary>
    ///   Checks whether the plan is empty for the specified phase.
    /// </summary>
    /// <param name="phase">
    ///   The phase to check.
    /// </param>
    /// <returns>
    ///   <see langword="true"/>
    ///     if the plan is empty for <paramref name="phase"/>, or
    ///     if every item in the plan is a pseudo-migration;
    ///   <see langword="false"/> otherwise.
    /// </returns>
    /// <remarks>
    ///   This method ignores pseudo-migrations.  A plan must have at least one
    ///   non-pseudo migration to be non-empty.
    /// </remarks>
    public bool IsEmpty(MigrationPhase phase)
    {
        return phase switch
        {
            MigrationPhase.Pre  => Pre .FindIndex(IsNonPseudo) < 0,
            MigrationPhase.Core => Core.FindIndex(IsNonPseudo) < 0,
            MigrationPhase.Post => Post.FindIndex(IsNonPseudo) < 0,
            _ => throw new ArgumentOutOfRangeException(nameof(phase)),
        };
    }

    /// <summary>
    ///   Gets the sequence of items to run in the specified phase.
    /// </summary>
    /// <param name="phase">
    ///   The phase for which to get items to run.
    /// </param>
    /// <returns>
    ///   The sequence of items to run in <paramref name="phase"/>.
    /// </returns>
    public IEnumerable<(Migration Migration, MigrationPhase Phase)> GetItems(MigrationPhase phase)
    {
        return phase switch
        {
            MigrationPhase.Pre  => Pre .Select(m => (m, phase)),
            MigrationPhase.Core => Core,
            MigrationPhase.Post => Post.Select(m => (m, phase)),
            _ => throw new ArgumentOutOfRangeException(nameof(phase)),
        };
    }

    private static bool IsRequired((Migration Migration, MigrationPhase Phase) item)
        => item.Migration[item.Phase].IsRequired;

    private static bool IsNonPseudo(Migration m)
        => !m.IsPseudo;

    private static bool IsNonPseudo((Migration Migration, MigrationPhase Phase) x)
        => !x.Migration.IsPseudo;
}
