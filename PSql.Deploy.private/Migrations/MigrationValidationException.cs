// Copyright 2023 Subatomix Research Inc.
// SPDX-License-Identifier: ISC

using System.Runtime.Serialization;

namespace PSql.Deploy.Migrations;

/// <summary>
///   The exception thrown when one or more migrations fail validation and
///   cannot be applied.
/// </summary>
[Serializable]
public class MigrationValidationException : Exception
{
    private const string DefaultMessage
        = "One or more migrations failed validation. "
        + "Address the the errors, then try again.";

    /// <summary>
    ///   Initializes a new <see cref="MigrationValidationException"/>
    ///   instance.
    /// </summary>
    public MigrationValidationException()
        : base(DefaultMessage) { }

    // public MigrationValidationException(string? message)
    //     : base(message ?? DefaultMessage) { }

    // public MigrationValidationException(string? message, Exception? innerException)
    //     : base(message ?? DefaultMessage, innerException) { }

    /// <summary>
    ///   Initializes a new <see cref="MigrationValidationException"/>
    ///   instance with serialized data.
    /// </summary>
    /// <inheritdoc/>
    protected MigrationValidationException(SerializationInfo info, StreamingContext context)
        : base(info, context) { }
}
