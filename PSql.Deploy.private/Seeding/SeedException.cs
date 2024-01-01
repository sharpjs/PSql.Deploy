// Copyright 2023 Subatomix Research Inc.
// SPDX-License-Identifier: ISC

#if !NET8_0_OR_GREATER
using System.Runtime.Serialization;
#endif

namespace PSql.Deploy.Seeding;

/// <summary>
///   Represents an error that occurred during database seed application.
/// </summary>
#if !NET8_0_OR_GREATER
[Serializable]
#endif
public class SeedException : Exception
{
    /// <inheritdoc/>
    public SeedException()
        : base("One or more errors occurred during seed application.")
    { }

    /// <inheritdoc/>
    public SeedException(string? message)
        : base(message)
    { }

    /// <inheritdoc/>
    public SeedException(string? message, Exception? innerException)
        : base(message, innerException)
    { }

#if !NET8_0_OR_GREATER
    /// <inheritdoc/>
    protected SeedException(SerializationInfo info, StreamingContext context)
        : base(info, context)
    { }
#endif
}
