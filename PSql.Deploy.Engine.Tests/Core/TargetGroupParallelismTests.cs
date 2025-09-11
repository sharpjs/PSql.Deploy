// Copyright Subatomix Research Inc.
// SPDX-License-Identifier: MIT

namespace PSql.Deploy;

[TestFixture]
[FixtureLifeCycle(LifeCycle.SingleInstance)]
public class TargetGroupParallelismTests
{
    [Test]
    public void Construct_NullGlobal()
    {
        var group = new TargetGroup([_target]);

        Should.Throw<ArgumentNullException>(() =>
        {
            _ = new TargetGroupParallelism(null!, group, maxTargets: 1);
        });
    }

    [Test]
    public void Construct_NullGroup()
    {
        using var global = new GlobalParallelism(maxActions: 8, maxActionsPerTarget: 4);

        Should.Throw<ArgumentNullException>(() =>
        {
            _ = new TargetGroupParallelism(global, null!, maxTargets: 1);
        });
    }

    [Test]
    public void Construct_MaxTargetsOutOfRange()
    {
        using var global = new GlobalParallelism(maxActions: 8, maxActionsPerTarget: 4);
              var group  = new TargetGroup([_target]);

        Should.Throw<ArgumentOutOfRangeException>(() =>
        {
            _ = new TargetGroupParallelism(global, group, maxTargets: 0);
        });
    }

    [Test]
    public void MaxTargets_Get()
    {
        using var global = new GlobalParallelism(maxActions: 8, maxActionsPerTarget: 4);
              var group  = new TargetGroup([_target]);

        using var parallelism = new TargetGroupParallelism(global, group, maxTargets: 3);

        parallelism.MaxTargets.ShouldBe(3);
    }

    [Test]
    public void MaxActions_Get_LessThanGlobal()
    {
        using var global = new GlobalParallelism(maxActions: 8, maxActionsPerTarget: 4);
              var group  = new TargetGroup([_target], maxParallelism: 6, maxParallelismPerTarget: 4);

        using var parallelism = new TargetGroupParallelism(global, group, maxTargets: 2);

        parallelism.MaxActions.ShouldBe(6); // taken from group
    }

    [Test]
    public void MaxActions_Get_MoreThanGlobal()
    {
        using var global = new GlobalParallelism(maxActions: 8, maxActionsPerTarget: 4);
              var group  = new TargetGroup([_target], maxParallelism: 10, maxParallelismPerTarget: 4);

        using var parallelism = new TargetGroupParallelism(global, group, maxTargets: 2);

        parallelism.MaxActions.ShouldBe(8); // taken from global
    }

    [Test]
    public void MaxActionsPerTarget_Get_LessThanGlobal()
    {
        using var global = new GlobalParallelism(maxActions: 8, maxActionsPerTarget: 4);
              var group  = new TargetGroup([_target], maxParallelism: 8, maxParallelismPerTarget: 3);

        using var parallelism = new TargetGroupParallelism(global, group, maxTargets: 2);

        parallelism.MaxActionsPerTarget.ShouldBe(3); // taken from group
    }

    [Test]
    public void MaxActionsPerTarget_Get_MoreThanGlobal()
    {
        using var global = new GlobalParallelism(maxActions: 8, maxActionsPerTarget: 4);
              var group  = new TargetGroup([_target], maxParallelism: 8, maxParallelismPerTarget: 6);

        using var parallelism = new TargetGroupParallelism(global, group, maxTargets: 2);

        parallelism.MaxActionsPerTarget.ShouldBe(4); // taken from global
    }

    [Test]
    public void MaxActionsPerTarget_Get_MoreThanMaxActions()
    {
        using var global = new GlobalParallelism(maxActions: 3, maxActionsPerTarget: 5);
              var group  = new TargetGroup([_target], maxParallelism: 4, maxParallelismPerTarget: 6);

        using var parallelism = new TargetGroupParallelism(global, group, maxTargets: 2);

        parallelism.MaxActionsPerTarget.ShouldBe(3); // taken from effective max actions
    }

    [Test]
    public void ForTarget_Get()
    {
        using var global = new GlobalParallelism(maxActions: 3, maxActionsPerTarget: 5);
              var group  = new TargetGroup([_target], maxParallelism: 4, maxParallelismPerTarget: 6);

        using var parallelism = new TargetGroupParallelism(global, group, maxTargets: 2);

        parallelism.ForTarget           .ShouldNotBeNull();
        parallelism.ForTarget           .ShouldBeSameAs(parallelism.ForTarget);
        parallelism.ForTarget.MaxActions.ShouldBe(3); // taken from max actions per target
    }

    [Test]
    public async Task BeginTargetScopeAsync()
    {
        using var global = new GlobalParallelism(maxActions: 8, maxActionsPerTarget: 4);
              var group  = new TargetGroup([_target], maxParallelism: 8, maxParallelismPerTarget: 4);

        using var parallelism = new TargetGroupParallelism(global, group, maxTargets: 2);

        using var scope0     = await parallelism.BeginTargetScopeAsync(CancellationToken.None);
        using var scope1     = await parallelism.BeginTargetScopeAsync(CancellationToken.None);
              var scope2Task =       parallelism.BeginTargetScopeAsync(CancellationToken.None);

        await Task.WhenAny(scope2Task, Task.Delay(10));
        scope2Task.IsCompleted.ShouldBeFalse();

        scope0.Dispose();
        using var scope2 = await scope2Task;
    }

    private readonly Target _target = new("Server = s; Database = d");
}
