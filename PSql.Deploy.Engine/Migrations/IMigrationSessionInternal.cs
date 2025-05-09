// Copyright Subatomix Research Inc.
// SPDX-License-Identifier: MIT

namespace PSql.Deploy.Migrations;

/// <summary>
///   Internal surface of a session in which schema migrations are applied to
///   target databases.
/// </summary>
internal interface IMigrationSessionInternal : IMigrationSession, IDeploymentSessionInternal
{
    /// <summary>
    ///   Gets the migrations applied to the specified target asynchronously.
    /// </summary>
    /// <param name="target">
    ///   An object representing a target database.
    /// </param>
    /// <returns>
    ///   A <see cref="Task"/> representing the asynchronous operation.  When
    ///   the task completes, its <see cref="Task{TResult}.Result"/> property
    ///   contains the migrations registered in the database specified by
    ///   <paramref name="target"/>.
    /// </returns>
    Task<IReadOnlyList<Migration>> GetAppliedMigrationsAsync(Target target);

    /// <summary>
    ///   Loads the specified migration's SQL content.
    /// </summary>
    /// <param name="migration">
    ///   The migration for which to load SQL content.
    /// </param>
    void LoadContent(Migration migration);
}
