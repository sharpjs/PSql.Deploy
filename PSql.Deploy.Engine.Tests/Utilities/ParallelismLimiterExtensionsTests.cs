// Copyright Subatomix Research Inc.
// SPDX-License-Identifier: MIT

namespace PSql.Deploy;

[TestFixture]
public class ParallelismLimiterExtensionsTests : TestHarnessBase
{
    private readonly Mock<IParallelismLimiter> _limiter;
    private readonly MockSequence              _sequence;

    public ParallelismLimiterExtensionsTests()
    {
        _limiter  = Mocks.Create<IParallelismLimiter>();
        _sequence = new();
    }

    [Test]
    public async Task BeginScopeAsync()
    {
        _limiter
            .InSequence(_sequence)
            .Setup(l => l.AcquireAsync(Cancellation.Token))
            .Returns(Task.CompletedTask)
            .Verifiable();

        _limiter
            .InSequence(_sequence)
            .Setup(l => l.Release())
            .Verifiable();

        using var scope = await _limiter.Object.BeginScopeAsync(Cancellation.Token);

        scope.ShouldBeOfType<ParallelismScope>();

        scope.Dispose(); // to test multiple disposal

        // scope will be disposed again here
    }
}
