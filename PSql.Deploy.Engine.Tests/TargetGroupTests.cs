// Copyright Subatomix Research Inc.
// SPDX-License-Identifier: MIT

namespace PSql.Deploy;

[TestFixture]
[FixtureLifeCycle(LifeCycle.SingleInstance)]
public class TargetGroupTests
{
    [Test]
    public void Construct_NullTargets()
    {
        Should.Throw<ArgumentNullException>(() =>
        {
            _ = new TargetGroup(targets: null!);
        });
    }

    [Test]
    public void Construct_NullTarget()
    {
        Should.Throw<ArgumentException>(() =>
        {
            _ = new TargetGroup(targets: [null!]);
        });
    }

    [Test]
    public void Construct_NegativeMaxParallelism()
    {
        Should.Throw<ArgumentOutOfRangeException>(() =>
        {
            _ = new TargetGroup([], maxParallelism: -1);
        });
    }

    [Test]
    public void Construct_NegativeMaxParallelismPerTarget()
    {
        Should.Throw<ArgumentOutOfRangeException>(() =>
        {
            _ = new TargetGroup([], maxParallelismPerTarget: -1);
        });
    }

    [Test]
    public void Targets_Get()
    {
        var target  = new Target("Database=db");
        var targets = new Target[] { target };

        new TargetGroup(targets).Targets.ShouldBeSameAs(targets);
    }

    [Test]
    public void Name_Get_Default()
    {
        new TargetGroup([]).Name.ShouldBeNull();
    }

    [Test]
    public void Name_Get_Explicit()
    {
        new TargetGroup([], "a").Name.ShouldBe("a");
    }

    [Test]
    public void MaxParallelism_Get_Default()
    {
        new TargetGroup([]).MaxParallelism.ShouldBe(Environment.ProcessorCount);
    }

    [Test]
    public void MaxParallelism_Get_Explicit()
    {
        new TargetGroup([], maxParallelism: 42).MaxParallelism.ShouldBe(42);
    }

    [Test]
    public void MaxParallelismPerTarget_Get_Default()
    {
        new TargetGroup([]).MaxParallelismPerTarget.ShouldBe(Environment.ProcessorCount);
    }

    [Test]
    public void MaxParallelismPerTarget_Get_Explicit()
    {
        new TargetGroup([], maxParallelismPerTarget: 42).MaxParallelismPerTarget.ShouldBe(42);
    }
}
