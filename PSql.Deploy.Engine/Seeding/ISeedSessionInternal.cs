// Copyright Subatomix Research Inc.
// SPDX-License-Identifier: MIT

namespace PSql.Deploy.Seeding;

/// <summary>
///   Internal surface of a session in which content seeds are applied to
///   target databases.
/// </summary>
internal interface ISeedSessionInternal : ISeedSession
{
    /// <summary>
    ///   Gets the token to monitor for cancellation requests.
    /// </summary>
    CancellationToken CancellationToken { get; }
}
