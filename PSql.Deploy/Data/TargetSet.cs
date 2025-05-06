// Copyright Subatomix Research Inc.
// SPDX-License-Identifier: MIT

namespace PSql.Deploy;

using System;
using System.Diagnostics;

/// <summary>
///   Represents a set of target databases with specified parallelism limits.
/// </summary>
[DebuggerDisplay(@"\{{Name}, Count = {Targets.Count}\}")]
public class TargetSet
{
    private readonly E.TargetSet _inner;

    /// <summary>
    ///   Initializes a new <see cref="TargetSet"/> instance by converting from
    ///   the specified object.
    /// </summary>
    /// <param name="obj">
    ///   The object to convert into a <see cref="TargetSet"/>.
    /// </param>
    /// <exception cref="ArgumentNullException">
    ///   <see langword="object"/> is <see langword="null"/>.
    /// </exception>
    /// <exception cref="ArgumentException">
    ///   <see langword="object"/> is not convertible to <see cref="TargetSet"/>.
    /// </exception>
    public TargetSet(object obj)
    {
        if (obj is null)
            throw new ArgumentNullException(nameof(obj));

        (_inner, Targets) = InitializeFrom(obj);
    }

    /// <summary>
    ///   Initializes a new <see cref="TargetSet"/> instance with the specified
    ///   values.
    /// </summary>
    /// <param name="targets">
    ///   The targets in the set.
    /// </param>
    /// <param name="name">
    ///   An optional name for the set.  If provided, PSql.Deploy uses the name
    ///   in command output and logs.
    /// </param>
    /// <param name="maxParallelism">
    ///   The maximum degree of parallelism across all targets in the set.
    ///   The special value <c>0</c> indicates parallelism equal to the count
    ///   of logical processors on the current machine.  Cannot be negative.
    /// </param>
    /// <param name="maxParallelismPerTarget">
    ///   The maximum degree of parallelism per target.  The special value
    ///   <c>0</c> indicates parallelism equal to the count of logical
    ///   processors on the current machine.  Cannot be negative.
    /// </param>
    /// <exception cref="ArgumentNullException">
    ///   <paramref name="targets"/> is <see langword="null"/>.
    /// </exception>
    /// <exception cref="ArgumentException">
    ///   <paramref name="targets"/> contains a <see langword="null"/> element.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    ///   <paramref name="maxParallelism"/> and/or
    ///   <paramref name="maxParallelismPerTarget"/> is negative.
    /// </exception>
    public TargetSet(
        IReadOnlyList<Target> targets,
        string?               name                    = null,
        int                   maxParallelism          = 0,
        int                   maxParallelismPerTarget = 0)
    {
        if (targets is null)
            throw new ArgumentNullException(nameof(targets));

        var innerTargets = ToInner(targets);

        _inner  = new(innerTargets, name, maxParallelism, maxParallelismPerTarget);
        Targets = targets;
    }

    /// <summary>
    ///   Gets the inner target set wrapped by this object.
    /// </summary>
    internal E.TargetSet InnerTargetSet => _inner;

    /// <summary>
    ///   Gets the target databases in the group.
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

    private static (E.TargetSet, IReadOnlyList<Target>)
        InitializeFrom(object obj)
    {
        if (obj is PSObject pSObject)
            obj = pSObject.BaseObject;

        if (obj is TargetSet targetSet)
            return (targetSet.InnerTargetSet, targetSet.Targets);

        if (obj is IReadOnlyList<Target> targetList)
            return InitializeFromTargetList(targetList);

        if (obj is ICollection collection)
            return InitializeFromCollection(collection);

        return InitializeFromOther(obj);
    }

    private static (E.TargetSet, IReadOnlyList<Target>)
        InitializeFromTargetList(IReadOnlyList<Target> targetList)
    {
        return (new(ToInner(targetList)), targetList);
    }

    private static (E.TargetSet, IReadOnlyList<Target>)
        InitializeFromCollection(ICollection collection)
    {
        return InitializeFromTargetList(ToTargetList(collection));
    }

    private static (E.TargetSet, IReadOnlyList<Target>)
        InitializeFromOther(object obj)
    {
        var target       = ToTarget(obj);
        var outerTargets = ImmutableArray.Create(target);
        var innerTargets = ImmutableArray.Create(target.InnerTarget);

        return (new(innerTargets), outerTargets);
    }

    private static Target ToTarget(object obj)
    {
        return obj as Target ?? new(obj);
    }

    private static IReadOnlyList<Target> ToTargetList(ICollection collection)
    {
        var array = ImmutableArray.CreateBuilder<Target>(collection.Count);

        foreach (var item in collection)
            array.Add(ToTarget(item));

        return array.MoveToImmutable();
    }

    private static IReadOnlyList<E.Target> ToInner(IReadOnlyList<Target> targets)
    {
        var array = ImmutableArray.CreateBuilder<E.Target>(targets.Count);

        foreach (var target in targets)
            array.Add(target.InnerTarget);

        return array.MoveToImmutable();
    }
}
