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
    ///   Loads the specified migration's SQL content.
    /// </summary>
    /// <param name="migration">
    ///   The migration for which to load SQL content.
    /// </param>
    /// <exception cref="ArgumentNullException">
    ///   <paramref name="migration"/> is <see langword="null"/>.
    /// </exception>
    /// <exception cref="ArgumentException">
    ///   <paramref name="migration"/> has a <see langword="null"/>
    ///   <see cref="Migration.Path"/>.
    /// </exception>
    void LoadContent(Migration migration);

    /// <summary>
    ///   Creates a connection to the specified target database.
    /// </summary>
    /// <param name="target">
    ///   An object that represents the target database.
    /// </param>
    /// <param name="logger">
    ///   The logger for server messages received over the connection.
    /// </param>
    /// <returns>
    ///   A connection to <paramref name="target"/>.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    ///   <paramref name="target"/> and/or
    ///   <paramref name="logger"/> is <see langword="null"/>.
    /// </exception>
    IMigrationTargetConnection Connect(Target target, ISqlMessageLogger logger);
}
