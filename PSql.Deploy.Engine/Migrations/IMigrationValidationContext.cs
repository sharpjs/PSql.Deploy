// Copyright Subatomix Research Inc.
// SPDX-License-Identifier: MIT

namespace PSql.Deploy.Migrations;

/// <summary>
///   Contextual information provided to <see cref="MigrationValidator"/>.
/// </summary>
internal interface IMigrationValidationContext
{
    /// <summary>
    ///   Gets the migration session.
    /// </summary>
    IMigrationSessionInternal Session { get; }

    /// <summary>
    ///   Gets an object representing the target database.
    /// </summary>
    Target Target { get; }
}
