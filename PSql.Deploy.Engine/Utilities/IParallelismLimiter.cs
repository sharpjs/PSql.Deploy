// Copyright Subatomix Research Inc.
// SPDX-License-Identifier: MIT

namespace PSql.Deploy;

/// <summary>
///   A governor to limit parallelism.
/// </summary>
public interface IParallelismLimiter
{
    /// <summary>
    ///   Gets the maximum degree of parallelism requested during construction.
    /// </summary>
    int RequestedLimit { get; }

    /// <summary>
    ///   Gets the maximum degree of parallelism actually in effect.
    /// </summary>
    int EffectiveLimit { get; }

    /// <summary>
    ///   Gets the number of units of parallelism that are available for use.
    /// </summary>
    int AvailableCount { get; }

    /// <summary>
    ///   Waits asynchronously to acquire one unit of parallelism.
    /// </summary>
    /// <param name="cancellation">
    ///   The token to monitor for cancellation requests.
    /// </param>
    /// <returns>
    ///   A task that represents the asynchronous operation.
    /// </returns>
    /// <exception cref="OperationCanceledException">
    ///   The operation was canceled via <paramref name="cancellation"/>.
    /// </exception>
    Task AcquireAsync(CancellationToken cancellation);

    /// <summary>
    ///   Releases one unit of parallelism.
    /// </summary>
    void Release();
}
