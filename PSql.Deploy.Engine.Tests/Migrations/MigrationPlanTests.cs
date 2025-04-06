// Copyright Subatomix Research Inc.
// SPDX-License-Identifier: MIT

namespace PSql.Deploy.Migrations;

[TestFixture]
public class MigrationPlanTests
{
    [Test]
    public void PendingMigrations_Get()
    {
        var migrations = ImmutableArray.Create<Migration>(new("a"), new("b"));

        new MigrationPlan(migrations).PendingMigrations.ShouldBe(migrations);
    }

    [Test]
    public void Pre_Get()
    {
        new MigrationPlan(default).Pre.ShouldBeEmpty();
    }

    [Test]
    public void Core_Get()
    {
        new MigrationPlan(default).Core.ShouldBeEmpty();
    }

    [Test]
    public void Post_Get()
    {
        new MigrationPlan(default).Post.ShouldBeEmpty();
    }

    [Test]
    public void IsCoreRequired_Get()
    {
        new MigrationPlan(default).IsCoreRequired.ShouldBeFalse();
    }

    [Test]
    public void IsCoreRequired_Set([Values(false, true)] bool expected)
    {
        new MigrationPlan(default) { IsCoreRequired = expected }
            .IsCoreRequired.ShouldBe(expected);
    }

    [Test]
    public void HasPreContentInCore_Get()
    {
        new MigrationPlan(default).HasPreContentInCore.ShouldBeFalse();
    }

    [Test]
    public void HasPreContentInCore_Set([Values(false, true)] bool expected)
    {
        new MigrationPlan(default) { HasPreContentInCore = expected }
            .HasPreContentInCore.ShouldBe(expected);
    }

    [Test]
    public void HasPostContentInCore_Get()
    {
        new MigrationPlan(default).HasPostContentInCore.ShouldBeFalse();
    }

    [Test]
    public void HasPostContentInCore_Set([Values(false, true)] bool expected)
    {
        new MigrationPlan(default) { HasPostContentInCore = expected }
            .HasPostContentInCore.ShouldBe(expected);
    }

    [Test]
    public void IsEmpty_Empty()
    {
        var plan = new MigrationPlan(default);

        plan.IsEmpty(MigrationPhase.Pre ).ShouldBeTrue();
        plan.IsEmpty(MigrationPhase.Core).ShouldBeTrue();
        plan.IsEmpty(MigrationPhase.Post).ShouldBeTrue();
    }

    [Test]
    public void IsEmpty_PseudoOnly()
    {
        var plan = new MigrationPlan(default);

        plan.Pre .Add( new(Migration.BeginPseudoMigrationName));
        plan.Core.Add((new(Migration.BeginPseudoMigrationName), MigrationPhase.Core));
        plan.Post.Add( new(Migration.BeginPseudoMigrationName));

        plan.IsEmpty(MigrationPhase.Pre ).ShouldBeTrue();
        plan.IsEmpty(MigrationPhase.Core).ShouldBeTrue();
        plan.IsEmpty(MigrationPhase.Post).ShouldBeTrue();
    }

    [Test]
    public void IsEmpty_NonPseudoPre()
    {
        var plan = new MigrationPlan(default);

        plan.Pre.Add(new("a"));

        plan.IsEmpty(MigrationPhase.Pre ).ShouldBeFalse();
        plan.IsEmpty(MigrationPhase.Core).ShouldBeTrue();
        plan.IsEmpty(MigrationPhase.Post).ShouldBeTrue();
    }

    [Test]
    public void IsEmpty_NonPseudoCore()
    {
        var plan = new MigrationPlan(default);

        plan.Core.Add((new("a"), MigrationPhase.Core));

        plan.IsEmpty(MigrationPhase.Pre ).ShouldBeTrue();
        plan.IsEmpty(MigrationPhase.Core).ShouldBeFalse();
        plan.IsEmpty(MigrationPhase.Post).ShouldBeTrue();
    }

    [Test]
    public void IsEmpty_NonPseudoPost()
    {
        var plan = new MigrationPlan(default);

        plan.Post.Add(new("a"));

        plan.IsEmpty(MigrationPhase.Pre ).ShouldBeTrue();
        plan.IsEmpty(MigrationPhase.Core).ShouldBeTrue();
        plan.IsEmpty(MigrationPhase.Post).ShouldBeFalse();
    }

    [Test]
    public void IsEmpty_OutOfRange()
    {
        const MigrationPhase InvalidPhase = (MigrationPhase) (-1);

        Should.Throw<ArgumentOutOfRangeException>(() =>
        {
            new MigrationPlan(default).IsEmpty(InvalidPhase);
        });
    }

    [Test]
    public void GetItems_Pre()
    {
        var plan      = new MigrationPlan(default);
        var migration = new Migration("a");

        plan.Pre.Add(migration);

        plan.GetItems(MigrationPhase.Pre ).ShouldBe([(migration, MigrationPhase.Pre)]);
        plan.GetItems(MigrationPhase.Core).ShouldBeEmpty();
        plan.GetItems(MigrationPhase.Post).ShouldBeEmpty();
    }

    [Test]
    public void GetItems_Core()
    {
        var plan      = new MigrationPlan(default);
        var migration = new Migration("a");

        plan.Core.Add((migration, MigrationPhase.Core));

        plan.GetItems(MigrationPhase.Pre ).ShouldBeEmpty();
        plan.GetItems(MigrationPhase.Core).ShouldBe([(migration, MigrationPhase.Core)]);
        plan.GetItems(MigrationPhase.Post).ShouldBeEmpty();
    }

    [Test]
    public void GetItems_Post()
    {
        var plan      = new MigrationPlan(default);
        var migration = new Migration("a");

        plan.Post.Add(migration);

        plan.GetItems(MigrationPhase.Pre ).ShouldBeEmpty();
        plan.GetItems(MigrationPhase.Core).ShouldBeEmpty();
        plan.GetItems(MigrationPhase.Post).ShouldBe([(migration, MigrationPhase.Post)]);
    }

    [Test]
    public void GetItems_OutOfRange()
    {
        const MigrationPhase InvalidPhase = (MigrationPhase) (-1);

        Should.Throw<ArgumentOutOfRangeException>(() =>
        {
            return new MigrationPlan(default).GetItems(InvalidPhase);
        });
    }
}
