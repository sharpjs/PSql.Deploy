// Copyright 2023 Subatomix Research Inc.
// SPDX-License-Identifier: ISC

namespace PSql.Deploy.Migrations;

[TestFixture]
public class MigrationPlanTests
{
    [Test]
    public void PendingMigrations_Get()
    {
        var migrations = ImmutableArray.Create<Migration>(new("a"), new("b"));

        new MigrationPlan(migrations).PendingMigrations.Should().Equal(migrations);
    }

    [Test]
    public void Pre_Get()
    {
        new MigrationPlan(default).Pre.Should().BeEmpty();
    }

    [Test]
    public void Core_Get()
    {
        new MigrationPlan(default).Core.Should().BeEmpty();
    }

    [Test]
    public void Post_Get()
    {
        new MigrationPlan(default).Post.Should().BeEmpty();
    }

    [Test]
    public void IsCoreRequired_Get()
    {
        new MigrationPlan(default).IsCoreRequired.Should().BeFalse();
    }

    [Test]
    public void IsCoreRequired_Set([Values(false, true)] bool expected)
    {
        new MigrationPlan(default) { IsCoreRequired = expected }
            .IsCoreRequired.Should().Be(expected);
    }

    [Test]
    public void HasPreContentInCore_Get()
    {
        new MigrationPlan(default).HasPreContentInCore.Should().BeFalse();
    }

    [Test]
    public void HasPreContentInCore_Set([Values(false, true)] bool expected)
    {
        new MigrationPlan(default) { HasPreContentInCore = expected }
            .HasPreContentInCore.Should().Be(expected);
    }

    [Test]
    public void HasPostContentInCore_Get()
    {
        new MigrationPlan(default).HasPostContentInCore.Should().BeFalse();
    }

    [Test]
    public void HasPostContentInCore_Set([Values(false, true)] bool expected)
    {
        new MigrationPlan(default) { HasPostContentInCore = expected }
            .HasPostContentInCore.Should().Be(expected);
    }

    [Test]
    public void IsEmpty_Empty()
    {
        var plan = new MigrationPlan(default);

        plan.IsEmpty(MigrationPhase.Pre ).Should().BeTrue();
        plan.IsEmpty(MigrationPhase.Core).Should().BeTrue();
        plan.IsEmpty(MigrationPhase.Post).Should().BeTrue();
    }

    [Test]
    public void IsEmpty_PseudoOnly()
    {
        var plan = new MigrationPlan(default);

        plan.Pre .Add( new(Migration.BeginPseudoMigrationName));
        plan.Core.Add((new(Migration.BeginPseudoMigrationName), MigrationPhase.Core));
        plan.Post.Add( new(Migration.BeginPseudoMigrationName));

        plan.IsEmpty(MigrationPhase.Pre ).Should().BeTrue();
        plan.IsEmpty(MigrationPhase.Core).Should().BeTrue();
        plan.IsEmpty(MigrationPhase.Post).Should().BeTrue();
    }

    [Test]
    public void IsEmpty_NonPseudoPre()
    {
        var plan = new MigrationPlan(default);

        plan.Pre.Add(new("a"));

        plan.IsEmpty(MigrationPhase.Pre ).Should().BeFalse();
        plan.IsEmpty(MigrationPhase.Core).Should().BeTrue();
        plan.IsEmpty(MigrationPhase.Post).Should().BeTrue();
    }

    [Test]
    public void IsEmpty_NonPseudoCore()
    {
        var plan = new MigrationPlan(default);

        plan.Core.Add((new("a"), MigrationPhase.Core));

        plan.IsEmpty(MigrationPhase.Pre ).Should().BeTrue();
        plan.IsEmpty(MigrationPhase.Core).Should().BeFalse();
        plan.IsEmpty(MigrationPhase.Post).Should().BeTrue();
    }

    [Test]
    public void IsEmpty_NonPseudoPost()
    {
        var plan = new MigrationPlan(default);

        plan.Post.Add(new("a"));

        plan.IsEmpty(MigrationPhase.Pre ).Should().BeTrue();
        plan.IsEmpty(MigrationPhase.Core).Should().BeTrue();
        plan.IsEmpty(MigrationPhase.Post).Should().BeFalse();
    }

    [Test]
    public void IsEmpty_OutOfRange()
    {
        new MigrationPlan(default)
            .Invoking(p => p.IsEmpty((MigrationPhase) (-1)))
            .Should().Throw<ArgumentOutOfRangeException>();
    }

    [Test]
    public void GetItems_Pre()
    {
        var plan      = new MigrationPlan(default);
        var migration = new Migration("a");

        plan.Pre.Add(migration);

        plan.GetItems(MigrationPhase.Pre ).Should().Equal((migration, MigrationPhase.Pre));
        plan.GetItems(MigrationPhase.Core).Should().BeEmpty();
        plan.GetItems(MigrationPhase.Post).Should().BeEmpty();
    }

    [Test]
    public void GetItems_Core()
    {
        var plan      = new MigrationPlan(default);
        var migration = new Migration("a");

        plan.Core.Add((migration, MigrationPhase.Core));

        plan.GetItems(MigrationPhase.Pre ).Should().BeEmpty();
        plan.GetItems(MigrationPhase.Core).Should().Equal((migration, MigrationPhase.Core));
        plan.GetItems(MigrationPhase.Post).Should().BeEmpty();
    }

    [Test]
    public void GetItems_Post()
    {
        var plan      = new MigrationPlan(default);
        var migration = new Migration("a");

        plan.Post.Add(migration);

        plan.GetItems(MigrationPhase.Pre ).Should().BeEmpty();
        plan.GetItems(MigrationPhase.Core).Should().BeEmpty();
        plan.GetItems(MigrationPhase.Post).Should().Equal((migration, MigrationPhase.Post));
    }

    [Test]
    public void GetItems_OutOfRange()
    {
        new MigrationPlan(default)
            .Invoking(p => p.GetItems((MigrationPhase) (-1)))
            .Should().Throw<ArgumentOutOfRangeException>();
    }
}
