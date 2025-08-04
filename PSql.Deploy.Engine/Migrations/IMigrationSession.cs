// Copyright Subatomix Research Inc.
// SPDX-License-Identifier: MIT

namespace PSql.Deploy.Migrations;

/// <summary>
///   A session in which schema migrations are applied to target databases.
/// </summary>
public interface IMigrationSession : IDeploymentSession
{
    /// <summary>
    ///   Gets the user interface via which the session reports progress.
    /// </summary>
    IMigrationConsole Console { get; }

    /// <summary>
    ///   Gets the phases in which session applies migrations.
    /// </summary>
    public MigrationPhaseSet EnabledPhases { get; }

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
    bool AllowContentInCorePhase { get; }

    /// <summary>
    ///   Gets the defined migrations.
    /// </summary>
    /// <remarks>
    ///   The default value is <see cref="ImmutableArray{T}.Empty"/>.
    ///   Invoke <see cref="DiscoverMigrations"/> to populate this property.
    /// </remarks>
    ImmutableArray<Migration> Migrations { get; }

    /// <summary>
    ///   Gets the earliest (minimum) name of the migrations in
    ///   <see cref="Migrations"/>, excluding the <c>_Begin</c> and <c>_End</c>
    ///   pseudo-migrations, if present.  Returns an empty string if
    ///   <see cref="Migrations"/> is empty or contains only pseudo-migrations.
    /// </summary>
    /// <remarks>
    ///   The default value is an empty string.
    ///   Invoke <see cref="DiscoverMigrations"/> to populate this property.
    /// </remarks>
    string EarliestDefinedMigrationName { get; }

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
    /// <remarks>
    ///   <para>
    ///     This method populates the <see cref="Migrations"/> and
    ///     <see cref="EarliestDefinedMigrationName"/> properties.
    ///   </para>
    ///   <para>
    ///     Invoke this method only when migration application is not
    ///     in progress.  Otherwise, this method throws
    ///     <see cref="InvalidOperationException"/>.
    ///   </para>
    /// </remarks>
    /// <exception cref="ArgumentNullException">
    ///   <paramref name="path"/> is <see langword="null"/>.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    ///   The method was invoked while migration application was in progress.
    /// </exception>
    /// <exception cref="IOException">
    ///   An input/output error occured while discovering migrations.
    /// </exception>
    void DiscoverMigrations(
        string  path,
        string? latestName = null);

    /// <summary>
    ///   Discovers the migrations registered in the specified target database
    ///   asynchronously.
    /// </summary>
    /// <param name="target">
    ///   An object representing a target database.
    /// </param>
    /// <param name="earliestName">
    ///   The earliest (minimum) name of migrations to discover, or
    ///   <see langword="null"/> to discover all migrations.
    /// </param>
    /// <param name="logger">
    ///   The logger for server messages received from the target database, or
    ///   <see langword="null"/> to disable logging.
    /// </param>
    /// <returns>
    ///   A <see cref="Task"/> representing the asynchronous operation.  When
    ///   the task completes, its <see cref="Task{TResult}.Result"/> property
    ///   contains the migrations registered in the database specified by
    ///   <paramref name="target"/>.
    /// </returns>
    Task<IReadOnlyList<Migration>> GetRegisteredMigrationsAsync(
        Target             target,
        string?            earliestName = null,
        ISqlMessageLogger? logger       = null
    );
}
