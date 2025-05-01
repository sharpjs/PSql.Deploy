// Copyright Subatomix Research Inc.
// SPDX-License-Identifier: MIT

namespace PSql.Deploy;

using System;

/// <summary>
///   Represents a set of target databases with specified parallelism limits.
/// </summary>
public class TargetSet
{
    private readonly E.TargetSet _inner;

    internal TargetSet(
        IReadOnlyList<Target> targets,
        string?               name                    = null,
        int                   maxParallelism          = 0,
        int                   maxParallelismPerTarget = 0)
    {
        var innerTargets = Unwrap(targets);

        _inner = new(innerTargets, name, maxParallelism, maxParallelismPerTarget);

        Targets = targets;
    }

    /// <summary>
    ///   Gets the inner target set wrapped by this object.
    /// </summary>
    internal E.TargetSet InnerTargetSet => _inner;

    /// <summary>
    ///   Gets the targets in the set.
    /// </summary>
    public IReadOnlyList<Target> Targets { get; }

    /// <summary>
    ///   Gets the descriptive name for the set, if any.
    /// </summary>
    public string? Name => _inner.Name;

    /// <summary>
    ///   Gets the maximum degree of parallelism across the entire set.
    /// </summary>
    public int MaxParallelism => _inner.MaxParallelism;

    /// <summary>
    ///   Gets the maximum degree of parallelism per database.
    /// </summary>
    public int MaxParallelismPerDatabase => _inner.MaxParallelismPerDatabase;

    private static IReadOnlyList<E.Target> Unwrap(IReadOnlyList<Target> targets)
    {
        if (targets is null)
            throw new ArgumentNullException(nameof(targets));

        var array = ImmutableArray.CreateBuilder<E.Target>(targets.Count);

        foreach (var target in targets)
            array.Add(target.InnerTarget);

        return array.MoveToImmutable();
    }
}
