// Copyright Subatomix Research Inc.
// SPDX-License-Identifier: MIT

namespace PSql.Deploy;

internal class CompositeParallelismLimiter : IParallelismLimiter
{
    private readonly IParallelismLimiter _limiter0;
    private readonly IParallelismLimiter _limiter1;

    public CompositeParallelismLimiter(IParallelismLimiter limiter0, IParallelismLimiter limiter1)
    {
        ArgumentNullException.ThrowIfNull(limiter0);
        ArgumentNullException.ThrowIfNull(limiter1);

        _limiter0 = limiter0;
        _limiter1 = limiter1;
    }

    /// <inheritdoc/>
    public int RequestedLimit
        => Math.Max(_limiter0.RequestedLimit, _limiter1.RequestedLimit);

    /// <inheritdoc/>
    public int EffectiveLimit
        => Math.Min(_limiter0.EffectiveLimit, _limiter1.EffectiveLimit);

    /// <inheritdoc/>
    public int AvailableCount
        => Math.Min(_limiter0.AvailableCount, _limiter1.AvailableCount);

    /// <inheritdoc/>
    public async Task AcquireAsync(CancellationToken cancellation)
    {
        await _limiter0.AcquireAsync(cancellation);

        try
        {
            await _limiter1.AcquireAsync(cancellation);
        }
        catch
        {
            BestEffort.Do(static l => l.Release(), _limiter0);
            throw;
        }
    }

    /// <inheritdoc/>
    public void Release()
    {
        BestEffort.Do(static l => l.Release(), _limiter1);
        BestEffort.Do(static l => l.Release(), _limiter0);
    }
}
