// Copyright Subatomix Research Inc.
// SPDX-License-Identifier: MIT

namespace PSql.Deploy;

/// <summary>
///   A no-op <see cref="IParallelismLimiter"/> implementation that imposes no
///   restrictions on parallelism.
/// </summary>
internal class NullParallelismLimiter : IParallelismLimiter
{
    /// <summary>
    ///   Gets the singleton <see cref="NullParallelismLimiter"/> instance.
    /// </summary>
    public static NullParallelismLimiter Instance { get; } = new();

    /// <inheritdoc/>
    public int RequestedLimit => 1; // because composite limiter takes max of requested

    /// <inheritdoc/>
    public int EffectiveLimit => int.MaxValue;

    /// <inheritdoc/>
    public int AvailableCount => int.MaxValue;

    /// <inheritdoc/>
    public Task AcquireAsync(CancellationToken cancellation)
        => Task.CompletedTask;

    /// <inheritdoc/>
    public void Release() { }
}
