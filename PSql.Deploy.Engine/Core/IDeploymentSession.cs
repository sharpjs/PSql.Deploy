// Copyright Subatomix Research Inc.
// SPDX-License-Identifier: MIT

namespace PSql.Deploy;

/// <summary>
///   A database deployment session.
/// </summary>
public interface IDeploymentSession : IDisposable
{
    /// <summary>
    ///   Gets whether the session operates in what-if mode.  In this mode, the
    ///   session reports what actions it would perform against a target
    ///   database but does not perform the actions.
    /// </summary>
    bool IsWhatIfMode { get; }

    /// <summary>
    ///   Gets whether the session has encountered an error.
    /// </summary>
    bool HasErrors { get; }

    /// <summary>
    ///   Begins applying the deployment operation to the specified group of
    ///   target databases.
    /// </summary>
    /// <param name="group">
    ///   The group of target databases.
    /// </param>
    /// <exception cref="ArgumentNullException">
    ///   <paramref name="group"/> is <see langword="null"/>.
    /// </exception>
    /// <remarks>
    ///   This method returns immediately.  The deployment operation to the
    ///   <paramref name="group"/> of target databases occurs asynchronously
    ///   and in parallel with other groups.
    /// </remarks>
    void BeginApplying(TargetGroup group);

    /// <summary>
    ///   Begins applying the deployment operation to the specified target
    ///   database.
    /// </summary>
    /// <param name="target">
    ///   The group of target databases.
    /// </param>
    /// <param name="maxParallelism">
    ///   The maximum degree of parallelism to use.  The special value <c>0</c>
    ///   indicates parallelism equal to the count of logical processors on the
    ///   current machine.  Cannot be negative.
    /// </param>
    /// <exception cref="ArgumentNullException">
    ///   <paramref name="target"/> is <see langword="null"/>.
    /// </exception>
    /// <remarks>
    ///   This method returns immediately.  The deployment operation to the
    ///   <paramref name="target"/> database occurs asynchronously and in
    ///   parallel with other targets.
    /// </remarks>
    void BeginApplying(Target target, int maxParallelism = 0);

    /// <summary>
    ///   Completes the deployment operation asynchronously.
    /// </summary>
    /// <param name="cancellation">
    ///   A token to monitor for cancellation requests.
    /// </param>
    /// <returns>
    ///   A <see cref="Task"/> representing the asynchronous operation.
    /// </returns>
    Task CompleteApplyingAsync(CancellationToken cancellation = default);

    /// <summary>
    ///   Cancels the deployment operation if it is in progress.
    /// </summary>
    void Cancel();
}
