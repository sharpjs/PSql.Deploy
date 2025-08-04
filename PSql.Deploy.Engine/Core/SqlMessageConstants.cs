// Copyright Subatomix Research Inc.
// SPDX-License-Identifier: MIT

namespace PSql.Deploy;

/// <summary>
///   Constants related to server messages received over connections to
///   target databases.
/// </summary>
public static class SqlMessageConstants
{
    /// <summary>
    ///   The maximum severity level of an informational or advisory message.
    ///   Messages of higher severity are errors.
    /// </summary>
    public const int MaxInformationalSeverity = 10;
}
