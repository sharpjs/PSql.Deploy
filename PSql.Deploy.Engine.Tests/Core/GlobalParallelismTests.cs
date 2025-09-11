// Copyright Subatomix Research Inc.
// SPDX-License-Identifier: MIT

namespace PSql.Deploy;

[TestFixture]
[FixtureLifeCycle(LifeCycle.SingleInstance)]
public class GlobalParallelismTests
{
    [Test]
    public void Construct_MaxActionsOutOfRange()
    {
        Should.Throw<ArgumentOutOfRangeException>(() =>
        {
            _ = new GlobalParallelism(maxActions: 0, maxActionsPerTarget: 4);
        });
    }

    [Test]
    public void Construct_MaxActionsPerTargetOutOfRange()
    {
        Should.Throw<ArgumentOutOfRangeException>(() =>
        {
            _ = new GlobalParallelism(maxActions: 8, maxActionsPerTarget: 0);
        });
    }

    [Test]
    public void ActionLimiter_Get()
    {
        using var parallelism = new GlobalParallelism(maxActions: 8, maxActionsPerTarget: 4);

        parallelism
            .ActionLimiter .ShouldBeOfType<ParallelismLimiter>()
            .EffectiveLimit.ShouldBe(8);
    }

    [Test]
    public void MaxActions_Get()
    {
        using var parallelism = new GlobalParallelism(maxActions: 8, maxActionsPerTarget: 4);

        parallelism.MaxActions.ShouldBe(8);
    }

    [Test]
    public void MaxActionsPerTarget_Get()
    {
        using var parallelism = new GlobalParallelism(maxActions: 8, maxActionsPerTarget: 4);

        parallelism.MaxActionsPerTarget.ShouldBe(4);
    }

    [Test]
    public void MaxActionsPerTarget_Get_Clamped()
    {
        using var parallelism = new GlobalParallelism(maxActions: 3, maxActionsPerTarget: 4);

        parallelism.MaxActionsPerTarget.ShouldBe(3);
    }

    [Test]
    public void ForGroup_NullGroup()
    {
        using var parallelism = new GlobalParallelism(maxActions: 8, maxActionsPerTarget: 4);

        Should.Throw<ArgumentNullException>(() =>
        {
            parallelism.ForGroup(group: null!, maxTargets: 2);
        });
    }

    [Test]
    public void ForGroup_MaxTargetsOutOfRange()
    {
        using var parallelism = new GlobalParallelism(maxActions: 8, maxActionsPerTarget: 4);
        
        var group = new TargetGroup([new("Server=s; Database=d")]);

        Should.Throw<ArgumentOutOfRangeException>(() =>
        {
            parallelism.ForGroup(group, maxTargets: 0);
        });
    }

    [Test]
    public void ForGroup_Ok()
    {
        using var parallelism = new GlobalParallelism(maxActions: 8, maxActionsPerTarget: 4);
        
        var group = new TargetGroup([new("Server=s; Database=d")]);

        using var forGroup = parallelism.ForGroup(group, maxTargets: 2);

        forGroup                    .ShouldBeOfType<TargetGroupParallelism>();
        forGroup.MaxTargets         .ShouldBe(2);
        forGroup.MaxActionsPerTarget.ShouldBe(4);
    }

    [Test]
    public void Dispose()
    {
        var parallelism = new GlobalParallelism(maxActions: 8, maxActionsPerTarget: 4);

        parallelism.Dispose();

        // Verify limiter got disposed
        Should.Throw<ObjectDisposedException>(() =>
        {
            parallelism.ActionLimiter.AcquireAsync(default).GetAwaiter().GetResult();
        });

        // Test multiple disposal
        parallelism.Dispose();
    }
}
