// Copyright Subatomix Research Inc.
// SPDX-License-Identifier: MIT

namespace PSql.Deploy.Migrations;

/// <summary>
///   Options for <see cref="MigrationSession"/>.
/// </summary>
[Flags]
public enum MigrationSessionOptions
{
    /// <summary>
    ///   Apply migration content in the <b>Pre</b>-deployment phase, if any
    ///   such content exists.
    /// </summary>
    /// <remarks>
    ///   This phase supports zero-downtime deployment scenarios.  Migration
    ///   content in the Pre phase runs <b>before</b> application deployment,
    ///   while the previous version of the application is running.
    /// </remarks>
    /// <seealso cref="MigrationPhase.Pre"/>
    PrePhase = 1 << MigrationPhase.Pre,

    /// <summary>
    ///   Apply migration content in the <b>Core</b> phase, if any such content
    ///   exists.
    /// </summary>
    /// <remarks>
    ///   ⚠ PSql.Deploy interprets migration content in the Core phase as
    ///   requiring application downtime, explicitly breaking a zero-downtime
    ///   deployment scenario.  As a safety measure, PSql.Deploy disallows
    ///   content in the Core phase by default.  To opt in to Core content,
    ///   use the <see cref="AllowContentInCorePhase"/> option.
    /// </remarks>
    /// <seealso cref="MigrationPhase.Core"/>
    CorePhase = 1 << MigrationPhase.Core,

    /// <summary>
    ///   Apply migration content in the <b>Post</b>-deployment phase, if any
    ///   such content exists.
    /// </summary>
    /// <remarks>
    ///   This phase supports zero-downtime deployment scenarios.  Migration
    ///   content in the Post phase runs <b>after</b> application deployment,
    ///   while the new version of the application is running.
    /// </remarks>
    /// <seealso cref="MigrationPhase.Post"/>
    PostPhase = 1 << MigrationPhase.Post,

    /// <summary>
    ///   Apply migration content in all phases for which content exists.
    /// </summary>
    AllPhases = PrePhase | CorePhase | PostPhase,

    /// <summary>
    ///   Allow migration content in the <b>Core</b> phase.
    /// </summary>
    /// <remarks>
    ///   ⚠ PSql.Deploy interprets migration content in the Core phase as
    ///   requiring application downtime, explicitly breaking a zero-downtime
    ///   deployment scenario.  As a safety measure, PSql.Deploy disallows
    ///   content in the Core phase by default.  This option removes that
    ///   safety measure.
    /// </remarks>
    AllowContentInCorePhase = 1 << 3,

    /// <summary>
    ///   Operate in what-if mode.  In this mode, a migration session reports
    ///   what actions it would perform against a target database but does not
    ///   perform the actions.
    /// </summary>
    IsWhatIfMode = 1 << 31,
}
