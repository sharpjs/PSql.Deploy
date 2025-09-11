// Copyright Subatomix Research Inc.
// SPDX-License-Identifier: MIT

namespace PSql.Deploy;

/// <summary>
///   A scope that holds one unit of parallelism acquired from an
///   <see cref="IParallelismLimiter"/> via
///   <see cref="ParallelismLimiterExtensions.BeginScopeAsync"/>.
/// </summary>
public sealed class ParallelismScope : IDisposable
{
    private IParallelismLimiter? _limiter;

    /// <summary>
    ///   Intializes a new <see cref="ParallelismScope"/> instance.
    /// </summary>
    /// <param name="limiter">
    ///   The parallelism limiter from which the scope was acquired.
    /// </param>
    internal ParallelismScope(IParallelismLimiter limiter)
    {
        _limiter = limiter;
    }

    /// <summary>
    ///   Releases the unit of parallelism held by the scope.
    /// </summary>
    public void Dispose()
    {
        Interlocked.Exchange(ref _limiter, null)?.Release();
    }
}
