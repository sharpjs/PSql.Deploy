// Copyright Subatomix Research Inc.
// SPDX-License-Identifier: MIT

namespace PSql.Deploy.Migrations;

/// <summary>
///   A resolvable reference to a database schema migration.
/// </summary>
public class MigrationReference
{
    /// <summary>
    ///   Initializes a new <see cref="MigrationReference"/> instance
    ///   referencing the specified name.
    /// </summary>
    /// <param name="name">
    ///   The migration name to reference.
    /// </param>
    /// <exception cref="ArgumentNullException">
    ///   <paramref name="name"/> is <see langword="null"/>.
    /// </exception>
    public MigrationReference(string name)
    {
        if (name is null)
            throw new ArgumentNullException(nameof(name));

        Name = name;
    }

    /// <summary>
    ///   Gets the referenced migration name.
    /// </summary>
    public string Name { get; }

    /// <summary>
    ///   Gets the referenced migration, or <see langword="null"/> if the
    ///   reference has not been resolved.
    /// </summary>
    public Migration? Migration { get; internal set; }
}
