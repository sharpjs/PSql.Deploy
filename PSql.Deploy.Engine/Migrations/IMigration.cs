// Copyright Subatomix Research Inc.
// SPDX-License-Identifier: MIT

namespace PSql.Deploy.Migrations;

/// <summary>
///   Information about a database schema migration.
/// </summary>
public interface IMigration
{
    // TODO: Should thre be an ISeed for symmetry?

    /// <summary>
    ///   Gets the name of the migration.
    /// </summary>
    public string Name { get; }

    /// <summary>
    ///   Gets whether the migration is a <c>_Begin</c> or <c>_End</c>
    ///   pseudo-migration.
    /// </summary>
    public bool IsPseudo { get; }

    /// <summary>
    ///   Gets the full path <c>_Main.sql</c> file of the migration, or
    ///   <see langword="null"/> if no path is known.
    /// </summary>
    public string? Path { get; }

    /// <summary>
    ///   Gets the hash computed from the SQL content of the migration, or an
    ///   empty string if no hash is known.
    /// </summary>
    public string Hash { get; }

    /// <summary>
    ///   Gets the application state of the migration.
    /// </summary>
    public MigrationState State { get; }
}
