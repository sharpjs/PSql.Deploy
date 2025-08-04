// Copyright Subatomix Research Inc.
// SPDX-License-Identifier: MIT

namespace PSql.Deploy.Seeds;

/// <summary>
///   Internal surface of a session in which content seeds are applied to
///   target databases.
/// </summary>
internal interface ISeedSessionInternal : ISeedSession, IDeploymentSessionInternal
{
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
    ISeedTargetConnection Connect(Target target, ISqlMessageLogger logger);
}
