// Copyright 2023 Subatomix Research Inc.
// SPDX-License-Identifier: ISC

namespace PSql.Deploy.Migrations;

/// <summary>
///   Migration content for a particular phase.
/// </summary>
public class MigrationContent
{
    /// <summary>
    ///   Gets the SQL script, or <see langword="null"/> if no script is known.
    ///   The default value is <see langword="null"/>.
    /// </summary>
    public string? Sql { get; internal set; }

    /// <summary>
    ///   Gets whether execution is required (i.e. cannot be skipped).
    ///   The default value is <see langword="false"/>.
    /// </summary>
    /// <remarks>
    ///   Execution is required if <see cref="Sql"/> contains authored SQL.
    ///   Execution is optional if <see cref="Sql"/> is <see langword="null"/>
    ///   or contains only the generated SQL that marks the migration as applied.
    /// </remarks>
    public bool IsRequired { get; internal set; } 
}
