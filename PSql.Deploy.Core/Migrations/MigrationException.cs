// Copyright 2023 Subatomix Research Inc.
// SPDX-License-Identifier: ISC

using System.Runtime.Serialization;

namespace PSql.Deploy.Migrations;

/// <summary>
///   Represents an error that occurred during database schema migration.
/// </summary>
[Serializable]
public class MigrationException : Exception
{
    /// <inheritdoc/>
    public MigrationException()
        : base("One or more errors occurred during migration.")
    { }

    /// <inheritdoc/>
    public MigrationException(string? message)
        : base(message)
    { }

    /// <inheritdoc/>
    public MigrationException(string? message, Exception? innerException)
        : base(message, innerException)
    { }

    /// <inheritdoc/>
    protected MigrationException(SerializationInfo info, StreamingContext context)
        : base(info, context)
    { }
}
