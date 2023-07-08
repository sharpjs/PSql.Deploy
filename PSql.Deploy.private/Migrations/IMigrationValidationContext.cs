// Copyright 2023 Subatomix Research Inc.
// SPDX-License-Identifier: ISC

namespace PSql.Deploy.Migrations;

/// <summary>
///   Contextual information provided to <see cref="MigrationValidator"/>.
/// </summary>
internal interface IMigrationValidationContext
{
    /// <summary>
    ///   Gets the phase in which migrations are being applied.
    /// </summary>
    MigrationPhase Phase { get; }

    /// <summary>
    ///   Gets a display name for the database server.  This name might be a
    ///   DNS name, an Azure resource name, or a placeholder indicating a local
    ///   SQL Server instance.
    /// </summary>
    string ServerName { get; }

    /// <summary>
    ///   Gets a short name for the database.  This name might be a real
    ///   database name or a placeholder indicating the default database for
    ///   the connection.
    /// </summary>
    string DatabaseName { get; }

    /// <inheritdoc cref="MigrationEngine.MinimumMigrationName"/>
    string EarliestDefinedMigrationName { get; }
}
