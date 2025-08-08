// Copyright Subatomix Research Inc.
// SPDX-License-Identifier: MIT

#if !NET8_0_OR_GREATER
using System.Runtime.Serialization;
#endif

namespace PSql.Deploy.Seeds;

using static ExceptionHelpers;

/// <summary>
///   Represents an error that occurred during database seed application.
/// </summary>
#if !NET8_0_OR_GREATER
[Serializable]
#endif
public class SeedException : Exception
{
    private const string
        DefaultMessage = "One or more errors occurred during seed application.";

    /// <inheritdoc/>
    public SeedException()
        : base(DefaultMessage)
    { }

    /// <inheritdoc/>
    public SeedException(string? message)
        : base(message ?? DefaultMessage)
    { }

    /// <inheritdoc/>
    public SeedException(string? message, Exception? innerException)
        : base(GetCompoundMessage(message ?? DefaultMessage, innerException), innerException)
    { }

#if !NET8_0_OR_GREATER
    /// <inheritdoc/>
    protected SeedException(SerializationInfo info, StreamingContext context)
        : base(info, context)
    { }
#endif
}
