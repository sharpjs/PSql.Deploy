// Copyright 2023 Subatomix Research Inc.
// SPDX-License-Identifier: ISC

namespace PSql.Deploy.Migrations;

/// <summary>
///   Internals of the migration system.
/// </summary>
/// <remarks>
///   This interface exists to form a mockable boundary between code units.
/// </remarks>
internal interface IMigrationInternals
{
    // TODO: Think of something more elegant than this.

    /// <summary>
    ///   Loads the specified migration's SQL content.
    /// </summary>
    /// <param name="migration">
    ///   The migration for which to load SQL content.
    /// </param>
    void LoadContent(Migration migration);

    /// <summary>
    ///   Opens a connection as determined by the property values of the
    ///   current context, optionally with the specified database name, logging
    ///   server messages with the specified logger.
    /// </summary>
    /// <param name="databaseName">
    ///   A database name.  If not <see langword="null"/>, this parameter
    ///   overrides the value of the <see cref="DatabaseName"/> property.
    /// </param>
    /// <param name="logger">
    ///   The object to use to log server messages received over the
    ///   connection.
    /// </param>
    /// <returns>
    ///   An object representing the open connection.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    ///   <paramref name="logger"/> is <see langword="null"/>.
    /// </exception>
    ISqlConnection Connect(SqlContext context, ISqlMessageLogger logger);
}
