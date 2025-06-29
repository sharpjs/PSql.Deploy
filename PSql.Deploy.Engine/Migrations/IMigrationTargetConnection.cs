// Copyright Subatomix Research Inc.
// SPDX-License-Identifier: MIT

namespace PSql.Deploy.Migrations;

/// <summary>
///   A connection to a target database for schema migration operations.
/// </summary>
internal interface IMigrationTargetConnection : ITargetConnection
{
    /// <summary>
    ///   Gets schema migrations registered in the target database
    ///   asynchronously.
    /// </summary>
    /// <param name="minimumName">
    ///   The minimum name of migrations to return.  This method ignores any
    ///   migration whose name is less than this minimum.  To return all
    ///   migrations, specify <see langword="null"/> or an empty string.
    ///   Comparisons use the default collation of the target database.
    /// </param>
    /// <param name="cancellation">
    ///   A token to monitor for cancellation requests.
    /// </param>
    /// <returns>
    ///   A <see cref="Task"/> representing the asynchronous operation.
    /// </returns>
    /// <remarks>
    ///   This method is safe to invoke even if schema migration support has
    ///   not yet been initialized in the target database.
    /// </remarks>
    Task<IReadOnlyList<Migration>> GetAppliedMigrationsAsync(
        string?           minimumName,
        CancellationToken cancellation = default
    );

    /// <summary>
    ///   Initializes schema migration support in the target database
    ///   asynchronously.
    /// </summary>
    /// <param name="cancellation">
    ///   A token to monitor for cancellation requests.
    /// </param>
    /// <returns>
    ///   A <see cref="Task"/> representing the asynchronous operation.
    /// </returns>
    /// <remarks>
    ///   This method creates or updates database objects necessary to apply
    ///   schema migrations to the target database.
    /// </remarks>
    Task InitializeMigrationSupportAsync(
        CancellationToken cancellation = default
    );

    /// <summary>
    ///   Executes the specified migration content against the target database
    ///   asynchronously.
    /// </summary>
    /// <param name="migration">
    ///   The migration containing the content to execute.
    /// </param>
    /// <param name="phase">
    ///   The phase of the migration content to execute.
    /// </param>
    /// <param name="cancellation">
    ///   A token to monitor for cancellation requests.
    /// </param>
    /// <returns>
    ///   A <see cref="Task"/> representing the asynchronous operation.
    /// </returns>
    /// <remarks>
    ///   This method requires prior initialization of schema migration support
    ///   in the target database.  Invoke
    ///   <see cref="InitializeMigrationSupportAsync"/> to initialize support.
    /// </remarks>
    /// <exception cref="ArgumentNullException">
    ///   <paramref name="migration"/> is <see langword="null"/>.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    ///   <paramref name="phase"/> is not a valid <see cref="MigrationPhase"/>.
    /// </exception>
    Task ExecuteMigrationContentAsync(
        Migration         migration,
        MigrationPhase    phase,
        CancellationToken cancellation = default
    );
}
