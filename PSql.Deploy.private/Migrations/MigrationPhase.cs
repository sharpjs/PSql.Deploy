// Copyright Subatomix Research Inc.
// SPDX-License-Identifier: MIT

namespace PSql.Deploy.Migrations;

/// <summary>
///   A database migration phase: a named moment during a deployment when
///   migration scripts are prescribed to run.
/// </summary>
public enum MigrationPhase
{
    /// <summary>
    ///   The <b>Pre</b>-deployment phase.
    /// </summary>
    /// <remarks>
    ///   This phase supports zero-downtime deployment scenarios.  Migration
    ///   scripts in the Pre phase run <b>before</b> application deployment,
    ///   while the previous version of the application is running.
    /// </remarks>
    Pre = M.MigrationPhase.Pre,

    /// <summary>
    ///   The <b>Core</b> deployment phase.
    /// </summary>
    /// <remarks>
    ///   ⚠ PSql.Deploy interprets a migration script in the Core phase as
    ///   requiring application downtime, explicitly breaking a zero-downtime
    ///   deployment scenario.
    /// </remarks>
    Core = M.MigrationPhase.Core,

    /// <summary>
    ///   The <b>Post</b>-deployment phase.
    /// </summary>
    /// <remarks>
    ///   This phase supports zero-downtime deployment scenarios.  Migration
    ///   scripts in the Post phase run <b>after</b> application deployment,
    ///   while the new version of the application is running.
    /// </remarks>
    Post = M.MigrationPhase.Post,
}
