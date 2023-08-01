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
    /// <summary>
    ///   Loads the specified migration's SQL content.
    /// </summary>
    /// <param name="migration">
    ///   The migration for which to load SQL content.
    /// </param>
    void LoadContent(Migration migration);

    ISqlConnection Connect(SqlContext context, IConsole logConsole);
}
