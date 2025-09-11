// Copyright Subatomix Research Inc.
// SPDX-License-Identifier: MIT

namespace PSql.Deploy;

/// <summary>
///   Extension methods for <see cref="IParallelismLimiter"/>.
/// </summary>
internal static class ParallelismLimiterExtensions
{
    /// <summary>
    ///   Establishes a scope occupying one unit of parallelism acquired from
    ///   the limiter.
    /// </summary>
    /// <param name="limiter">
    ///   The limiter from which to acquire the unit of parallelism.
    /// </param>
    /// <param name="cancellation">
    ///   The token to monitor for cancellation requests.
    /// </param>
    /// <returns>
    ///   A task that represents the asynchronous operation. The task result is
    ///   an object that releases the unit of parallelism on disposal.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    ///   <paramref name="limiter"/> is <see langword="null"/>.
    /// </exception>
    /// <exception cref="OperationCanceledException">
    ///   The operation was canceled via <paramref name="cancellation"/>.
    /// </exception>
    /// <exception cref="ObjectDisposedException">
    ///   <paramref name="limiter"/> has been disposed.
    /// </exception>
    public static async Task<ParallelismScope> BeginScopeAsync(
        this IParallelismLimiter limiter,
        CancellationToken        cancellation = default)
    {
        ArgumentNullException.ThrowIfNull(limiter);

        await limiter.AcquireAsync(cancellation).ConfigureAwait(false);

        return new(limiter);
    }
}
