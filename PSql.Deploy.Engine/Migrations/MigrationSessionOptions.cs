// Copyright Subatomix Research Inc.
// SPDX-License-Identifier: MIT

namespace PSql.Deploy.Migrations;

/// <summary>
///   Options for a session in which schema migrations are applied to target
///   databases.
/// </summary>
public class MigrationSessionOptions : DeploymentSessionOptions
{
    /// <summary>
    ///   Gets or sets the phases in which the session should apply migrations,
    ///   or <see langword="null"/> to specify all phases.  The default value
    ///   is <see langword="null"/>.
    /// </summary>
    /// <remarks>
    ///   <see cref="MigrationSession"/> requires this property either to be
    ///   <see langword="null"/> or to specify at least one migration phase.
    ///   To avoid an exception, ensure that the value of this property is not
    ///   an empty collection.
    /// </remarks>
    public IEnumerable<MigrationPhase>? EnabledPhases { get; set; }

    /// <summary>
    ///   Gets or sets whether the session should allow migration content in
    ///   the <b>Core</b> phase.  The default value is <see langword="false"/>.
    /// </summary>
    /// <remarks>
    ///   ⚠ PSql.Deploy interprets migration content in the Core phase as
    ///   requiring application downtime, explicitly breaking a zero-downtime
    ///   deployment scenario.  As a safety measure, PSql.Deploy disallows
    ///   content in the Core phase by default.  Setting this property to
    ///   <see langword="true"/> removes that safety measure.
    /// </remarks>
    public bool AllowContentInCorePhase { get; set; }
}
