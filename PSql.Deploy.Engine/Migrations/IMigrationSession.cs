// Copyright Subatomix Research Inc.
// SPDX-License-Identifier: MIT

namespace PSql.Deploy.Migrations;

/// <summary>
///   A session in which schema migrations are applied to target databases.
/// </summary>
public interface IMigrationSession : IDeploymentSession
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
    ///   Gets the user interface via which the session reports progress.
    /// </summary>
    IMigrationConsole Console { get; }

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
    ///     This method populates the
    ///     <see cref="IMigrationSession.Migrations"/> and
    ///     <see cref="IMigrationSession.EarliestDefinedMigrationName"/>
    ///     properties.
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
    void DiscoverMigrations(string path, string? latestName = null);
}
