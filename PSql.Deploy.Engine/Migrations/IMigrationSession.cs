// Copyright Subatomix Research Inc.
// SPDX-License-Identifier: MIT

namespace PSql.Deploy.Migrations;

/// <summary>
///   Information about a session in which schema migrations are applied to
///   target databases.
/// </summary>
/// <remarks>
///   This interface provides a read-only view of session information.  Code
///   that controls operation of the session should use the
///   <see cref="IMigrationSessionControl"/> interface instead.
/// </remarks>
public interface IMigrationSession
{
    /// <summary>
    ///   Gets whether the session applies migration content in the specified
    ///   phase, if any such content exists.
    /// </summary>
    /// <param name="phase">
    ///   The phase to check.
    /// </param>
    /// <returns>
    ///   <see langword="true"/> if the session applies migration content
    ///     in the <paramref name="phase"/> if any such content exists;
    ///   <see langword="false"/> otherwise.
    /// </returns>
    bool IsEnabled(MigrationPhase phase);

    /// <summary>
    ///   Gets the current phase.
    /// </summary>
    MigrationPhase CurrentPhase { get; }

    /// <summary>
    ///   Gets whether the session allows content in the <c>Core</c> phase.
    /// </summary>
    /// <remarks>
    ///   See remarks for
    ///   <see cref="MigrationSessionOptions.AllowContentInCorePhase"/>.
    /// </remarks>
    bool AllowContentInCorePhase { get; } // TODO: Mention 'non-skippable' again?

    /// <summary>
    ///   Gets whether the session operates in what-if mode.  In this mode, the
    ///   session reports what actions it would perform against a target
    ///   database but does not perform the actions.
    /// </summary>
    bool IsWhatIfMode { get; }

    /// <summary>
    ///   Gets the user interface via which the session reports progress.
    /// </summary>
    IMigrationConsole Console { get; }

    /// <summary>
    ///   Gets the defined migrations.
    /// </summary>
    /// <remarks>
    ///   The default value is <see cref="ImmutableArray{T}.Empty"/>.
    ///   Invoke <see cref="IMigrationSessionControl.DiscoverMigrations"/> to
    ///   populate this property.
    /// </remarks>
    ImmutableArray<IMigration> Migrations { get; }

    /// <summary>
    ///   Gets the earliest (minimum) name of the migrations in
    ///   <see cref="Migrations"/>, excluding the <c>_Begin</c> and <c>_End</c>
    ///   pseudo-migrations, if present.  Returns an empty string if
    ///   <see cref="Migrations"/> is empty or contains only pseudo-migrations.
    /// </summary>
    /// <remarks>
    ///   The default value is an empty string.
    ///   Invoke <see cref="IMigrationSessionControl.DiscoverMigrations"/> to
    ///   populate this property.
    /// </remarks>
    string EarliestDefinedMigrationName { get; }
}
