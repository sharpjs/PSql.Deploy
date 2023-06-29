// Copyright 2023 Subatomix Research Inc.
// SPDX-License-Identifier: ISC

namespace PSql.Deploy.Migrations;

/// <summary>
///   A plan for the order in which to run migrations.
/// </summary>
internal class MigrationPlan
{
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
    ///   Gets whether the migration plan requires downtime.
    /// </summary>
    public bool RequiresDowntime => Core.Count > 0;

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
}
