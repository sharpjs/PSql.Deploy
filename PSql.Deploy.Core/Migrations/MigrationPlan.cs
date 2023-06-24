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
    public List<(Migration, MigrationPhase)> Core { get; } = new();

    /// <summary>
    ///   Gets the migrations whose <c>Post</c> components actually will run
    ///   during the <c>Post</c> phase.
    /// </summary>
    public List<Migration> Post { get; } = new();

    /// <summary>
    ///   Gets whether the migration plan requires downtime.
    /// </summary>
    public bool RequiresDowntime => Core.Count > 0;
}
