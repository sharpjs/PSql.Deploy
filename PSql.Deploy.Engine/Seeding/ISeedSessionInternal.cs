// Copyright Subatomix Research Inc.
// SPDX-License-Identifier: MIT

namespace PSql.Deploy.Seeding;

/// <summary>
///   Internal surface of a session in which content seeds are applied to
///   target databases.
/// </summary>
internal interface ISeedSessionInternal : ISeedSession, IDeploymentSessionInternal
{
    // Empty
}
