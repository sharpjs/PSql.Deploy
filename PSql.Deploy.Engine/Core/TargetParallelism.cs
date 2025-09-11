// Copyright Subatomix Research Inc.
// SPDX-License-Identifier: MIT

namespace PSql.Deploy;

/// <summary>
///   A policy to manage parallelism within a target database.
/// </summary>
public class TargetParallelism
{
    private readonly IParallelismLimiter _actionLimiter;

    /// <summary>
    ///   Initializes a new <see cref="TargetParallelism"/> instance.
    /// </summary>
    /// <param name="actionLimiter">
    ///   The governor that limits the number of actions (such as SQL batches)
    ///   that can be executed in parallel against the target database.
    /// </param>
    /// <param name="maxActions">
    ///   The maximum number of actions (such as SQL batches) that a deployment
    ///   session should perform in parallel against the target database.
    /// </param>
    /// <exception cref="ArgumentNullException">
    ///   <paramref name="actionLimiter"/> is <see langword="null"/>.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    ///   <paramref name="maxActions"/> is zero or negative.
    /// </exception>
    public TargetParallelism(IParallelismLimiter actionLimiter, int maxActions)
    {
        ArgumentNullException.ThrowIfNull(actionLimiter);

        if (maxActions <= 0)
            throw new ArgumentOutOfRangeException(nameof(maxActions));

        _actionLimiter = actionLimiter;
        MaxActions     = maxActions;
    }

    /// <summary>
    ///   Gets the maximum number of actions (such as SQL batches) that a
    ///   deployment session should perform in parallel against the target
    ///   database.
    /// </summary>
    public int MaxActions { get; }

    /// <summary>
    ///   Establishes a scope asynchronously to perform one sequence of actions
    ///   (such as SQL batches) against the target database, occupying one unit
    ///   of action parallelism.
    /// </summary>
    /// <param name="cancellation">
    ///   The token to monitor for cancellation requests.
    /// </param>
    /// <returns>
    ///   A task that represents the asynchronous operation. The task result is
    ///   an object that releases the unit of action parallelism on disposal.
    /// </returns>
    /// <exception cref="OperationCanceledException">
    ///   The operation was canceled via <paramref name="cancellation"/>.
    /// </exception>
    /// <exception cref="ObjectDisposedException">
    ///   The object has been disposed.
    /// </exception>
    public Task<ParallelismScope> BeginActionScopeAsync(CancellationToken cancellation = default)
        => _actionLimiter.BeginScopeAsync(cancellation);
}
