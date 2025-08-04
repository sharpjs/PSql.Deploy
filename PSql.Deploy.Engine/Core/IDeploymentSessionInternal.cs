// Copyright Subatomix Research Inc.
// SPDX-License-Identifier: MIT

namespace PSql.Deploy;

/// <summary>
///   Internal surface of a database deployment session.
/// </summary>
internal interface IDeploymentSessionInternal : IDeploymentSession
{
    /// <summary>
    ///   Gets the token to monitor for cancellation requests.
    /// </summary>
    CancellationToken CancellationToken { get; }
}
