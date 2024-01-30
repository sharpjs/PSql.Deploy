// Copyright 2024 Subatomix Research Inc.
// SPDX-License-Identifier: ISC

#if !NET8_0_OR_GREATER
using System.Runtime.Serialization;
#endif

namespace PSql.Deploy.Migrations;

/// <summary>
///   Represents an error that occurred during database schema migration.
/// </summary>
#if !NET8_0_OR_GREATER
[Serializable]
#endif
public class MigrationException : Exception
{
    private const string
        DefaultMessage = "One or more errors occurred during migration.";

    /// <inheritdoc/>
    public MigrationException()
        : base(DefaultMessage)
    { }

    /// <inheritdoc/>
    public MigrationException(string? message)
        : base(message ?? DefaultMessage)
    { }

    /// <inheritdoc/>
    public MigrationException(string? message, Exception? innerException)
        : base(message ?? DefaultMessage, innerException)
    { }

#if !NET8_0_OR_GREATER
    /// <inheritdoc/>
    protected MigrationException(SerializationInfo info, StreamingContext context)
        : base(info, context)
    { }
#endif
}
