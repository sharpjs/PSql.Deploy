// Copyright Subatomix Research Inc.
// SPDX-License-Identifier: MIT

namespace PSql.Deploy;

using System;
using System.Diagnostics;

/// <summary>
///   Represents a set of target databases with specified parallelism limits.
/// </summary>
[DebuggerDisplay(@"\{{Name}, Count = {Targets.Count}\}")]
public class SqlTargetDatabaseGroup
{
    private readonly E.TargetGroup _inner;

    /// <summary>
    ///   Initializes a new <see cref="SqlTargetDatabaseGroup"/> instance by converting from
    ///   the specified object.
    /// </summary>
    /// <param name="obj">
    ///   The object to convert into a <see cref="SqlTargetDatabaseGroup"/>.
    /// </param>
    /// <exception cref="ArgumentNullException">
    ///   <see langword="object"/> is <see langword="null"/>.
    /// </exception>
    /// <exception cref="ArgumentException">
    ///   <see langword="object"/> is not convertible to <see cref="SqlTargetDatabaseGroup"/>.
    /// </exception>
    public SqlTargetDatabaseGroup(object obj)
    {
        if (obj is null)
            throw new ArgumentNullException(nameof(obj));

        (_inner, Targets) = InitializeFrom(obj);
    }

    /// <summary>
    ///   Initializes a new <see cref="SqlTargetDatabaseGroup"/> instance with the specified
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
    public SqlTargetDatabaseGroup(
        IReadOnlyList<SqlTargetDatabase> targets,
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
    internal E.TargetGroup InnerGroup => _inner;

    /// <summary>
    ///   Gets the target databases in the group.
    /// </summary>
    public IReadOnlyList<SqlTargetDatabase> Targets { get; }

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
    public int MaxParallelismPerDatabase => _inner.MaxParallelismPerTarget;

    private static (E.TargetGroup, IReadOnlyList<SqlTargetDatabase>)
        InitializeFrom(object obj)
    {
        if (obj is PSObject pSObject)
            obj = pSObject.BaseObject;

        if (obj is SqlTargetDatabaseGroup group)
            return (group.InnerGroup, group.Targets);

        if (obj is IReadOnlyList<SqlTargetDatabase> targetList)
            return InitializeFromTargetList(targetList);

        if (obj is ICollection collection)
            return InitializeFromCollection(collection);

        return InitializeFromOther(obj);
    }

    private static (E.TargetGroup, IReadOnlyList<SqlTargetDatabase>)
        InitializeFromTargetList(IReadOnlyList<SqlTargetDatabase> targetList)
    {
        return (new(ToInner(targetList)), targetList);
    }

    private static (E.TargetGroup, IReadOnlyList<SqlTargetDatabase>)
        InitializeFromCollection(ICollection collection)
    {
        return InitializeFromTargetList(ToTargetList(collection));
    }

    private static (E.TargetGroup, IReadOnlyList<SqlTargetDatabase>)
        InitializeFromOther(object obj)
    {
        var target       = ToTarget(obj);
        var outerTargets = ImmutableArray.Create(target);
        var innerTargets = ImmutableArray.Create(target.InnerTarget);

        return (new(innerTargets), outerTargets);
    }

    private static SqlTargetDatabase ToTarget(object obj)
    {
        return obj as SqlTargetDatabase ?? new(obj);
    }

    private static IReadOnlyList<SqlTargetDatabase> ToTargetList(ICollection collection)
    {
        var array = ImmutableArray.CreateBuilder<SqlTargetDatabase>(collection.Count);

        foreach (var item in collection)
            array.Add(ToTarget(item));

        return array.MoveToImmutable();
    }

    private static IReadOnlyList<E.Target> ToInner(IReadOnlyList<SqlTargetDatabase> targets)
    {
        var array = ImmutableArray.CreateBuilder<E.Target>(targets.Count);

        foreach (var target in targets)
            array.Add(target.InnerTarget);

        return array.MoveToImmutable();
    }

    /// <inheritdoc/>
    public override string ToString()
    {
        return string.IsNullOrEmpty(Name)
            ? DescribeTargets()
            : string.Format("{0} ({1})", Name, DescribeTargets());
    }

    private string DescribeTargets()
    {
        return Targets.Count switch
        {
            0     => "empty",
            1     =>    Targets[0].FullDisplayName,
            var n => $"{Targets[0].FullDisplayName} +{n - 1}"
        };
    }
}
