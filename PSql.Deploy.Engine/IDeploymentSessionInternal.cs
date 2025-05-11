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

    /// <summary>
    ///   Creates a connection to the specified target database.
    /// </summary>
    /// <param name="target">
    ///   An object that represents the target database.
    /// </param>
    /// <param name="logger">
    ///   The logger for server messages received over the connection.
    /// </param>
    /// <returns>
    ///   A connection to <paramref name="target"/>.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    ///   <paramref name="target"/> and/or
    ///   <paramref name="logger"/> is <see langword="null"/>.
    /// </exception>
    ITargetConnection Connect(Target target, ISqlMessageLogger logger);
}
