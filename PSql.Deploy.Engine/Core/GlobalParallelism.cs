// Copyright Subatomix Research Inc.
// SPDX-License-Identifier: MIT

namespace PSql.Deploy;

/// <summary>
///   A policy to manage parallelism within a database deployment session.
/// </summary>
internal class GlobalParallelism : IDisposable
{
    private readonly ParallelismLimiter _actionLimiter;

    /// <summary>
    ///   Initializes a new <see cref="GlobalParallelism"/> instance.
    /// </summary>
    /// <param name="maxActions">
    ///   The maximum number of actions (such as SQL batches) that a deployment
    ///   session should perform in parallel across all target databases.
    /// </param>
    /// <param name="maxActionsPerTarget">
    ///   The maximum number of actions (such as SQL batches) that a deployment
    ///   session should perform in parallel against any one target database.
    /// </param>
    /// <exception cref="ArgumentOutOfRangeException">
    ///   <paramref name="maxActions"/> and/or
    ///   <paramref name="maxActionsPerTarget"/> is zero or negative.
    /// </exception>
    public GlobalParallelism(int maxActions, int maxActionsPerTarget)
    {
        if (maxActions <= 0)
            throw new ArgumentOutOfRangeException(nameof(maxActions));
        if (maxActionsPerTarget <= 0)
            throw new ArgumentOutOfRangeException(nameof(maxActionsPerTarget));

        _actionLimiter      = new(maxActions);
        MaxActionsPerTarget = Math.Min(maxActions, maxActionsPerTarget);
    }

    /// <summary>
    ///   Gets the governor that limits the number of actions (such as SQL
    ///   batches) that can be executed in parallel across all target
    ///   databases.
    /// </summary>
    internal IParallelismLimiter ActionLimiter => _actionLimiter;

    /// <summary>
    ///   Gets the maximum number of actions (such as SQL batches) that a
    ///   deployment session should perform in parallel across all target
    ///   databases.
    /// </summary>
    public int MaxActions => ActionLimiter.EffectiveLimit;

    /// <summary>
    ///   Gets the maximum number of actions (such as SQL batches) that a
    ///   deployment session should perform in parallel against any one target
    ///   database.
    /// </summary>
    public int MaxActionsPerTarget { get; }

    /// <summary>
    ///   Derives a parallelism policy for the specified target database group.
    /// </summary>
    /// <param name="group">
    ///   The target database group for which to derive a parallelism policy.
    /// </param>
    /// <param name="maxTargets">
    ///   The maximum number of target databases from <paramref name="group"/>
    ///   against which a deployment session should operate in parallel.
    /// </param>
    /// <returns>
    ///   A parallelism policy for the specified <paramref name="group"/> with
    ///   the specified maximum number of parallel targets.
    /// </returns>
    public TargetGroupParallelism ForGroup(TargetGroup group, int maxTargets)
    {
        return new(this, group, maxTargets);
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        _actionLimiter.Dispose();
    }
}
