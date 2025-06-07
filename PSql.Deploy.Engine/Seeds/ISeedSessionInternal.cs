// Copyright Subatomix Research Inc.
// SPDX-License-Identifier: MIT

namespace PSql.Deploy.Seeds;

/// <summary>
///   Internal surface of a session in which content seeds are applied to
///   target databases.
/// </summary>
internal interface ISeedSessionInternal : ISeedSession, IDeploymentSessionInternal
{
    // Empty
}
