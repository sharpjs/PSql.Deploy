// Copyright Subatomix Research Inc.
// SPDX-License-Identifier: MIT

namespace PSql.Deploy;

/// <summary>
///   A governor to limit parallelism.
/// </summary>
internal sealed class ParallelismLimiter : IParallelismLimiter, IDisposable
{
    private readonly SemaphoreSlim _semaphore;

    /// <summary>
    ///   Initializes a new <see cref="ParallelismLimiter"/> instance with
    ///   the specified limit.
    /// </summary>
    /// <param name="limit">
    ///   The maximum degree of parallelism.  Must be positive.
    /// </param>
    /// <exception cref="ArgumentOutOfRangeException">
    ///   <paramref name="limit"/> is zero or negative.
    /// </exception>
    public ParallelismLimiter(int limit)
    {
        if (limit <= 0)
            throw new ArgumentOutOfRangeException(nameof(limit));

        RequestedLimit = limit;
        EffectiveLimit = limit;

        _semaphore = new(initialCount: limit, maxCount: limit);
    }

    /// <inheritdoc/>
    public int RequestedLimit { get; }

    /// <inheritdoc/>
    public int EffectiveLimit { get; }

    /// <inheritdoc/>
    public int AvailableCount => _semaphore.CurrentCount;

    /// <inheritdoc/>
    public Task AcquireAsync(CancellationToken cancellationToken)
        => _semaphore.WaitAsync(cancellationToken);

    /// <inheritdoc/>
    public void Release()
        => _semaphore.Release();

    /// <summary>
    ///   Releases the resources used by the object.
    /// </summary>
    public void Dispose()
        => _semaphore.Dispose();
}
