// Copyright Subatomix Research Inc.
// SPDX-License-Identifier: MIT

namespace PSql.Deploy.Migrations;

[TestFixture]
[FixtureLifeCycle(LifeCycle.SingleInstance)]
public class WhatIfMigrationStateTests
{
    [Test]
    public void Get_NullTarget()
    {
        var state = new WhatIfMigrationState();

        Should.Throw<ArgumentNullException>(() =>
        {
            state.Get(null!, []);
        });
    }

    [Test]
    public void Get_NullMigrations()
    {
        var state = new WhatIfMigrationState();

        Should.Throw<ArgumentNullException>(() =>
        {
            state.Get(null!, []);
        });
    }

    [Test]
    public void Get_Empty()
    {
        var state  = new WhatIfMigrationState();
        var target = new Target("Server=.;Database=X");

        state.Get(target, []).ShouldBeEmpty();
        state.Get(target, []).ShouldBeEmpty();
    }

    [Test]
    public void Get_RegisteredOnTarget()
    {
        var state      = new WhatIfMigrationState();
        var target     = new Target("Server=.;Database=X");
        var registered = new Migration("A") { Hash = "123" };

        state.Get(target, [registered])
            .ShouldHaveSingleItem()
            .AssignTo(out var returned);

        returned      .ShouldBeSameAs(registered);
        returned.State.ShouldBe(MigrationState.NotApplied);
    }

    [Test]
    public void Get_RegisteredOnTarget_PreviouslyAppliedInWhatIf()
    {
        var state      = new WhatIfMigrationState();
        var target     = new Target("Server=.;Database=X");
        var applied    = new Migration("A") { Hash = "123" };
        var registered = new Migration("A") { Hash = "123" };

        state.OnApplied(target, applied, MigrationPhase.Pre);

        state.Get(target, [registered])
            .ShouldHaveSingleItem()
            .AssignTo(out var returned);

        returned      .ShouldBeSameAs(registered);
        returned.State.ShouldBe(MigrationState.AppliedPre);
    }

    [Test]
    public void Get_PreviouslyAppliedInWhatIf()
    {
        var state   = new WhatIfMigrationState();
        var target  = new Target("Server=.;Database=X");
        var applied = new Migration("A") { Hash = "123" };

        state.OnApplied(target, applied, MigrationPhase.Pre);

        state.Get(target, [])
            .ShouldHaveSingleItem()
            .AssignTo(out var returned);

        returned      .ShouldNotBeSameAs(applied);
        returned.Name .ShouldBeSameAs(applied.Name);
        returned.Hash .ShouldBeSameAs(applied.Hash);
        returned.State.ShouldBe(MigrationState.AppliedPre);
    }

    [Test]
    public void OnApplied_NullTarget()
    {
        var state = new WhatIfMigrationState();

        Should.Throw<ArgumentNullException>(() =>
        {
            state.OnApplied(null!, new("A"), default);
        });
    }

    [Test]
    public void OnApplied_NullMigration()
    {
        var state = new WhatIfMigrationState();

        Should.Throw<ArgumentNullException>(() =>
        {
            state.OnApplied(new("Server=.;Database=A"), null!, default);
        });
    }

    [Test]
    public void OnApplied_AlreadyAppliedInSameOrLaterPhase()
    {
        var state     = new WhatIfMigrationState();
        var target    = new Target("Server=.;Database=A");
        var migration = new Migration("A") { State = MigrationState.AppliedCore };

        Should.Throw<ArgumentOutOfRangeException>(() =>
        {
            state.OnApplied(target, migration, MigrationPhase.Core);
        });
    }

    [Test]
    public void OnApplied_AlreadyAppliedInEarlierPhase()
    {
        var state    = new WhatIfMigrationState();
        var target   = new Target("Server=.;Database=A");
        var applied0 = new Migration("A") { Hash = "123", State = MigrationState.NotApplied };
        var applied1 = new Migration("A") { Hash = "123", State = MigrationState.AppliedPre };

        state.OnApplied(target, applied0, MigrationPhase.Pre);

        state.Get(target, []).ShouldHaveSingleItem().AssignTo(out var returned);
        returned     .ShouldNotBeSameAs(applied0);
        returned.Name.ShouldBeSameAs(applied0.Name);
        returned.Hash.ShouldBeSameAs(applied0.Hash);

        state.OnApplied(target, applied1, MigrationPhase.Core);

        state.Get(target, []).ShouldHaveSingleItem().AssignTo(out returned);
        returned     .ShouldNotBeSameAs(applied1);
        returned.Name.ShouldBeSameAs(applied1.Name);
        returned.Hash.ShouldBeSameAs(applied1.Hash);
    }
}
