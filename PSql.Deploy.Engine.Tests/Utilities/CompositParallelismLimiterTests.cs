// Copyright Subatomix Research Inc.
// SPDX-License-Identifier: MIT

namespace PSql.Deploy;

[TestFixture]
public class CompositParallelismLimiterTests : TestHarnessBase
{
    private readonly CompositeParallelismLimiter _limiter;
    private readonly Mock<IParallelismLimiter>   _limiter0;
    private readonly Mock<IParallelismLimiter>   _limiter1;

    public CompositParallelismLimiterTests()
    {
        _limiter0 = Mocks.Create<IParallelismLimiter>();
        _limiter1 = Mocks.Create<IParallelismLimiter>();

        _limiter  = new CompositeParallelismLimiter(_limiter0.Object, _limiter1.Object);
    }

    [Test]
    public void Construct_Limiter0Null()
    {
        Should.Throw<ArgumentNullException>(
            () => new CompositeParallelismLimiter(null!, _limiter1.Object)
        );
    }

    [Test]
    public void Construct_Limiter1Null()
    {
        Should.Throw<ArgumentNullException>(
            () => new CompositeParallelismLimiter(_limiter0.Object, null!)
        );
    }

    [Test]
    public void RequestedLimit_Get()
    {
        _limiter0.Setup(l => l.RequestedLimit).Returns(2);
        _limiter1.Setup(l => l.RequestedLimit).Returns(3);

        _limiter.RequestedLimit.ShouldBe(3); // max of requested
    }

    [Test]
    public void EffectiveLimit_Get()
    {
        _limiter0.Setup(l => l.EffectiveLimit).Returns(2);
        _limiter1.Setup(l => l.EffectiveLimit).Returns(3);

        _limiter.EffectiveLimit.ShouldBe(2); // min of effective
    }

    [Test]
    public void AvailableCount_Get()
    {
        _limiter0.Setup(l => l.AvailableCount).Returns(2);
        _limiter1.Setup(l => l.AvailableCount).Returns(3);

        _limiter.AvailableCount.ShouldBe(2); // min of available
    }

    [Test]
    public async Task Acquire_Ok()
    {
        _limiter0
            .Setup(l => l.AcquireAsync(Cancellation.Token))
            .Returns(Task.CompletedTask)
            .Verifiable();

        _limiter1
            .Setup(l => l.AcquireAsync(Cancellation.Token))
            .Returns(Task.CompletedTask)
            .Verifiable();

        await _limiter.AcquireAsync(Cancellation.Token);
    }

    [Test]
    public async Task Acquire_Limiter1Throws()
    {
        var exception = new Exception("Bang!");

        _limiter0
            .Setup(l => l.AcquireAsync(Cancellation.Token))
            .Returns(Task.CompletedTask)
            .Verifiable();

        _limiter1
            .Setup(l => l.AcquireAsync(Cancellation.Token))
            .ThrowsAsync(exception)
            .Verifiable();

        _limiter0.Setup(l => l.Release()).Verifiable();

        var thrown = await Should.ThrowAsync<Exception>(
            () => _limiter.AcquireAsync(Cancellation.Token)
        );

        thrown.ShouldBeSameAs(exception);
    }

    [Test]
    public void Release_Ok()
    {
        _limiter0.Setup(l => l.Release()).Verifiable();
        _limiter1.Setup(l => l.Release()).Verifiable();

        _limiter.Release();
    }

    [Test]
    public void Release_Limiter0Throws()
    {
        _limiter0.Setup(l => l.Release()).Throws<Exception>().Verifiable();
        _limiter1.Setup(l => l.Release()).Verifiable();

        _limiter.Release();
    }
}
