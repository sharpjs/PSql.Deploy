// Copyright Subatomix Research Inc.
// SPDX-License-Identifier: MIT

extern alias Engine;

using System.Net;

namespace PSql.Deploy;

using M = Engine::PSql.Deploy;

/// <inheritdoc cref="M.Target"/>
public class Target
{
    private readonly M.Target _target;

    /// <inheritdoc cref="M.Target.Target(string, NetworkCredential?, string?, string?)"/>
    public Target(
        string             connectionString,
        NetworkCredential? credential          = null,
        string?            serverDisplayName   = null,
        string?            databaseDisplayName = null)
    {
        _target = new(connectionString, credential, serverDisplayName, databaseDisplayName);
    }

    internal M.Target InnerTarget => _target;
}
