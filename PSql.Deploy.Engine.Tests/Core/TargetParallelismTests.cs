// Copyright Subatomix Research Inc.
// SPDX-License-Identifier: MIT

namespace PSql.Deploy;

[TestFixture]
[FixtureLifeCycle(LifeCycle.SingleInstance)]
public class TargetParallelismTests
{
    [Test]
    public void Construct_NullActionLimiter()
    {
        Should.Throw<ArgumentNullException>(() =>
        {
            _ = new TargetParallelism(null!, maxActions: 1);
        });
    }

    [Test]
    public void Construct_MaxActionsOutOfRange()
    {
        Should.Throw<ArgumentOutOfRangeException>(() =>
        {
            _ = new TargetParallelism(NullParallelismLimiter.Instance, maxActions: 0);
        });
    }

    [Test]
    public void MaxActions_Get()
    {
        var parallelism = new TargetParallelism(NullParallelismLimiter.Instance, maxActions: 3);

        parallelism.MaxActions.ShouldBe(3);
    }

    [Test]
    public async Task BeginActionScopeAsync()
    {
        using var limiter     = new ParallelismLimiter(2);
              var parallelism = new TargetParallelism(limiter, maxActions: 1);

        using var scope0     = await parallelism.BeginActionScopeAsync();
        using var scope1     = await parallelism.BeginActionScopeAsync();
              var scope2Task =       parallelism.BeginActionScopeAsync();

        await Task.WhenAny(scope2Task, Task.Delay(10));
        scope2Task.IsCompleted.ShouldBeFalse();

        scope0.Dispose();
        using var scope2 = await scope2Task;
    }
}
