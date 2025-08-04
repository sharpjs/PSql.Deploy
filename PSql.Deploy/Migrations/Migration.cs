// Copyright Subatomix Research Inc.
// SPDX-License-Identifier: MIT

using System.Diagnostics;

namespace PSql.Deploy.Migrations;

/// <summary>
///   A database schema migration.
/// </summary>
[DebuggerDisplay(@"\{{Name}, {State}\}")]
public class Migration
{
    private readonly M.Migration _inner;

    internal Migration(M.Migration inner)
    {
        ArgumentNullException.ThrowIfNull(inner);

        _inner = inner;
    }

    /// <summary>
    ///   Gets the name of the migration.
    /// </summary>
    public string Name => _inner.Name;

    /// <summary>
    ///   Gets whether the migration is a <c>_Begin</c> or <c>_End</c>
    ///   pseudo-migration.
    /// </summary>
    public bool IsPseudo => _inner.IsPseudo;

    /// <summary>
    ///   Gets the full path <c>_Main.sql</c> file of the migration, or
    ///   <see langword="null"/> if no path is known.
    /// </summary>
    public string? Path => _inner.Path;

    /// <summary>
    ///   Gets the hash computed from the SQL content of the migration, or an
    ///   empty string if no hash is known.
    /// </summary>
    public string Hash => _inner.Hash;

    /// <summary>
    ///   Gets the application state of the migration.
    /// </summary>
    public MigrationState State => (MigrationState) _inner.State;
}
