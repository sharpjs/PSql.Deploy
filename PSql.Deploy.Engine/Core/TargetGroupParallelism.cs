// Copyright Subatomix Research Inc.
// SPDX-License-Identifier: MIT

namespace PSql.Deploy;

/// <summary>
///   A policy to manage parallelism within a target database group.
/// </summary>
internal class TargetGroupParallelism : IDisposable
{
    private readonly  ParallelismLimiter  _targetLimiter;
    private readonly IParallelismLimiter  _actionLimiter;
    private readonly  ParallelismLimiter? _ownedActionLimiter;

    /// <summary>
    ///   Initializes a new <see cref="TargetGroupParallelism"/> instance.
    /// </summary>
    /// <param name="global">
    ///   The global parallelism policy.
    /// </param>
    /// <param name="group">
    ///   The target database group.
    /// </param>
    /// <param name="maxTargets">
    ///   The maximum number of target databases in <paramref name="group"/>
    ///   against which a deployment session should operate in parallel.
    /// </param>
    /// <exception cref="ArgumentNullException">
    ///   <paramref name="global"/> and/or
    ///   <paramref name="group"/> is <see langword="null"/>.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    ///   <paramref name="maxTargets"/> is zero or negative.
    /// </exception>
    public TargetGroupParallelism(GlobalParallelism global, TargetGroup group, int maxTargets)
    {
        ArgumentNullException.ThrowIfNull(global);
        ArgumentNullException.ThrowIfNull(group);

        if (maxTargets <= 0)
            throw new ArgumentOutOfRangeException(nameof(maxTargets));

        var maxActions          = group.MaxParallelism;
        var maxActionsPerTarget = group.MaxParallelismPerTarget;

        // Clamp limits
        maxActions          = Math.Min(maxActions,          global.MaxActions);
        maxActionsPerTarget = Math.Min(maxActionsPerTarget, global.MaxActionsPerTarget);
        maxActionsPerTarget = Math.Min(maxActionsPerTarget, maxActions);

        _targetLimiter = new(maxTargets);

        _actionLimiter = maxActions >= global.MaxActions
            ? global.ActionLimiter
            : new CompositeParallelismLimiter(
                _ownedActionLimiter = new(maxActions),
                global.ActionLimiter
            );

        ForTarget = new(_actionLimiter, maxActionsPerTarget);
    }

    /// <summary>
    ///   Gets the parallelism policy operation against any one target database
    ///   in the group.
    /// </summary>
    public TargetParallelism ForTarget { get; }

    /// <summary>
    ///   Gets the maximum number target databases in the group against which
    ///   a deployment session should operate in parallel.
    /// </summary>
    public int MaxTargets => _targetLimiter.EffectiveLimit;

    /// <summary>
    ///   Gets the maximum number of actions (such as SQL batches) that a
    ///   deployment session should perform in parallel across all target
    ///   databases in the group.
    /// </summary>
    public int MaxActions => _actionLimiter.EffectiveLimit;

    /// <summary>
    ///   Gets the maximum number of actions (such as SQL batches) that a
    ///   deployment session should perform in parallel against any one target
    ///   database in the group.
    /// </summary>
    public int MaxActionsPerTarget => ForTarget.MaxActions;

    /// <summary>
    ///   Establishes a scope asynchronously to perform database deployment
    ///   operations against one target database, occupying one unit of target
    ///   parallelism.
    /// </summary>
    /// <param name="cancellation">
    ///   The token to monitor for cancellation requests.
    /// </param>
    /// <returns>
    ///   A task that represents the asynchronous operation. The task result is
    ///   an object that releases the unit of target parallelism on disposal.
    /// </returns>
    /// <exception cref="OperationCanceledException">
    ///   The operation was canceled via <paramref name="cancellation"/>.
    /// </exception>
    /// <exception cref="ObjectDisposedException">
    ///   The object has been disposed.
    /// </exception>
    public Task<ParallelismScope> BeginTargetScopeAsync(CancellationToken cancellation = default)
        => _targetLimiter.BeginScopeAsync(cancellation);

    /// <inheritdoc/>
    public void Dispose()
    {
        _targetLimiter      .Dispose();
        _ownedActionLimiter?.Dispose();
    }
}
