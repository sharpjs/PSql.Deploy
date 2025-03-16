// Copyright Subatomix Research Inc.
// SPDX-License-Identifier: MIT

namespace PSql.Deploy.Migrations;

/// <summary>
///   Internal surface of a session in which schema migrations are applied to
///   target databases.
/// </summary>
internal interface IMigrationSessionInternal : IMigrationSession
{
    /// <summary>
    ///   Gets the token to monitor for cancellation requests.
    /// </summary>
    CancellationToken CancellationToken { get; }

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
}
