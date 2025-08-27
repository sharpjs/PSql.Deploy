// Copyright Subatomix Research Inc.
// SPDX-License-Identifier: MIT

namespace PSql.Deploy;

/// <summary>
///   Represents a set of target databases with specified parallelism limits.
/// </summary>
public class TargetGroup
{
    /// <summary>
    ///   Initializes a new <see cref="TargetGroup"/> instance.
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
    public TargetGroup(
        IReadOnlyList<Target> targets,
        string?               name                    = null,
        int                   maxParallelism          = 0,
        int                   maxParallelismPerTarget = 0)
    {
        if (targets is null)
            throw new ArgumentNullException(nameof(targets));
        if (targets.Contains(null!))
            throw new ArgumentException("Cannot contain a null element.", nameof(targets));
        if (maxParallelism < 0)
            throw new ArgumentOutOfRangeException(nameof(maxParallelism));
        if (maxParallelismPerTarget < 0)
            throw new ArgumentOutOfRangeException(nameof(maxParallelismPerTarget));

        Targets                 = targets;
        Name                    = name;
        MaxParallelism          = InterpretParallelism(maxParallelism);
        MaxParallelismPerTarget = InterpretParallelism(maxParallelismPerTarget);
    }

    /// <summary>
    ///   Gets the targets in the set.
    /// </summary>
    public IReadOnlyList<Target> Targets { get; }

    /// <summary>
    ///   Gets the descriptive name for the set, if any.
    /// </summary>
    public string? Name { get; }

    /// <summary>
    ///   Gets the maximum degree of parallelism across all target databases in
    ///   the group.
    /// </summary>
    public int MaxParallelism { get; }

    /// <summary>
    ///   Gets the maximum degree of parallelism per target database.
    /// </summary>
    public int MaxParallelismPerTarget { get; }

    private static int InterpretParallelism(int value)
        => value > 0 ? value : ProcessInfo.Instance.ProcessorCount;
}
