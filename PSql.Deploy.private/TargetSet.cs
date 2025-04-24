// Copyright Subatomix Research Inc.
// SPDX-License-Identifier: MIT

extern alias Engine;

namespace PSql.Deploy;

using M = Engine::PSql.Deploy;

public class TargetSet
{
    private readonly M.TargetSet _inner;

    public TargetSet()
    {
        _inner = new();
    }

    /// <summary>
    ///   Gets the inner <see cref="M.TargetSet"/> wrapped by this object.
    /// </summary>
    internal M.TargetSet InnerTargetSet => _inner;
}
