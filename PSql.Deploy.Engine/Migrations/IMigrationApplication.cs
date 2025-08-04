// Copyright Subatomix Research Inc.
// SPDX-License-Identifier: MIT

namespace PSql.Deploy.Migrations;

/// <summary>
///   Information about the application of schema migrations to a target
///   database.
/// </summary>
public interface IMigrationApplication
{
    /// <summary>
    ///   Gets the migration session.
    /// </summary>
    IMigrationSession Session { get; }

    /// <summary>
    ///   Gets an object representing the target database.
    /// </summary>
    Target Target { get; }
}
