// Copyright Subatomix Research Inc.
// SPDX-License-Identifier: MIT

namespace PSql.Deploy;

[TestFixture]
public class ParallelismLimiterTests
{
    [Test]
    public void Construct_LimitOutOfRange()
    {
        Should.Throw<ArgumentOutOfRangeException>(
            () => new ParallelismLimiter(0)
        );
    }

    [Test]
    public void Construct()
    {
        using var limiter = new ParallelismLimiter(2);

        limiter.RequestedLimit.ShouldBe(2);
        limiter.EffectiveLimit.ShouldBe(2);
        limiter.AvailableCount.ShouldBe(2);
    }

    [Test]
    public async Task AcquireAndRelease()
    {
        using var limiter = new ParallelismLimiter(2);

        limiter.AvailableCount.ShouldBe(2);

        await limiter.AcquireAsync(CancellationToken.None);
        limiter.AvailableCount.ShouldBe(1);

        await limiter.AcquireAsync(CancellationToken.None);
        limiter.AvailableCount.ShouldBe(0);

        var acquireTask = limiter.AcquireAsync(CancellationToken.None);
        await ShouldBeWaitingAsync(acquireTask);

        limiter.Release();
        await acquireTask;
        limiter.AvailableCount.ShouldBe(0);

        limiter.Release();
        limiter.AvailableCount.ShouldBe(1);

        limiter.Release();
        limiter.AvailableCount.ShouldBe(2);
    }

    [Test]
    public async Task Acquire_Canceled()
    {
        using var limiter      = new ParallelismLimiter(1);
        using var cancellation = new CancellationTokenSource();

        await limiter.AcquireAsync(cancellation.Token);
        limiter.AvailableCount.ShouldBe(0);

        var acquireTask = limiter.AcquireAsync(cancellation.Token);
        await ShouldBeWaitingAsync(acquireTask);

        cancellation.Cancel();
        await Should.ThrowAsync<OperationCanceledException>(() => acquireTask);
        limiter.AvailableCount.ShouldBe(0);

        limiter.Release();
        limiter.AvailableCount.ShouldBe(1);
    }

    [Test]
    public async Task Acquire_Disposed()
    {
        var limiter = new ParallelismLimiter(1);
        limiter.Dispose();

        await Should.ThrowAsync<ObjectDisposedException>(
            () => limiter.AcquireAsync(CancellationToken.None)
        );
    }

    [Test]
    public void Release_TooMany()
    {
        using var limiter = new ParallelismLimiter(1);

        Should.Throw<SemaphoreFullException>(limiter.Release);
        limiter.AvailableCount.ShouldBe(1);
    }

    [Test]
    public void Release_Disposed()
    {
        var limiter = new ParallelismLimiter(1);
        limiter.Dispose();

        Should.Throw<ObjectDisposedException>(limiter.Release);
    }

    [Test]
    public void Dispose_Multiple()
    {
        var limiter = new ParallelismLimiter(1);

        limiter.Dispose();
        limiter.Dispose();
    }

    private static async Task ShouldBeWaitingAsync(Task task)
    {
        (await Task.WhenAny(task, Task.Delay(10))).ShouldNotBeSameAs(task);
    }
}
