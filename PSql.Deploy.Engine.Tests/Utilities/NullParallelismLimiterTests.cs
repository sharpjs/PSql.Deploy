// Copyright Subatomix Research Inc.
// SPDX-License-Identifier: MIT

namespace PSql.Deploy;

[TestFixture]
public class NullParallelismLimiterTests
{
    [Test]
    public void Instance()
    {
        var limiter = NullParallelismLimiter.Instance;

        limiter.RequestedLimit.ShouldBe(1); // because composite limiter takes max of requested
        limiter.EffectiveLimit.ShouldBe(int.MaxValue);
        limiter.AvailableCount.ShouldBe(int.MaxValue);
    }

    [Test]
    public void Acquire()
    {
        NullParallelismLimiter
            .Instance.AcquireAsync(CancellationToken.None)
            .IsCompleted.ShouldBeTrue();
    }

    [Test]
    public void Release()
    {
        NullParallelismLimiter
            .Instance.Release();
    }
}
