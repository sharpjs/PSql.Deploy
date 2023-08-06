// Copyright 2023 Subatomix Research Inc.
// SPDX-License-Identifier: ISC

namespace PSql.Deploy.Migrations;

/// <summary>
///   Control surface for a session in which schema migrations are applied to a
///   set of target databases.
/// </summary>
/// <remarks>
///   This interface is used by higher-level code that controls the operation
///   of the session.  Lower-level code that consumes the session information
///   uses <see cref="IMigrationSession"/> instead.
/// </remarks>
public interface IMigrationSessionControl
{
    /// <summary>
    ///   Gets or sets the current deployment phase.  The default value is
    ///   <see cref="MigrationPhase.Pre"/>.
    /// </summary>
    MigrationPhase Phase { get; set; }

    /// <summary>
    ///   Gets or sets whether to allow a non-skippable <c>Core</c> phase.
    ///   The default value is <see langword="false"/>.
    /// </summary>
    bool AllowCorePhase { get; set; }

    /// <summary>
    ///   Gets or sets whether to operate in what-if mode.  In this mode, code
    ///   should report what actions it would perform against a target database
    ///   but should not perform the actions.  The default value is
    ///   <see langword="false"/>.
    /// </summary>
    bool IsWhatIfMode { get; set; }

    /// <summary>
    ///   Discovers migrations in the specified directory path.
    /// </summary>
    /// <param name="path">
    ///   The path of a directory in which to discover migrations.
    /// </param>
    /// <param name="latestName">
    ///   The latest (maximum) name of migrations to discover, or
    ///   <see langword="null"/> to discover all migrations.
    /// </param>
    void DiscoverMigrations(string path, string? latestName = null);

    /// <summary>
    ///   Applies any outstanding migrations for the current phase to the
    ///   specified target database asynchronously.
    /// </summary>
    /// <param name="target">
    ///   An object specifying the target database.
    /// </param>
    /// <param name="console">
    ///   The console on which to report progress.
    /// </param>
    /// <returns>
    ///   A <see cref="Task"/> representing the asynchronous operation.
    /// </returns>
    Task ApplyAsync(SqlContextWork target, IConsole console);
}
