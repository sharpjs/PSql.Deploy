// Copyright Subatomix Research Inc.
// SPDX-License-Identifier: MIT

namespace PSql.Deploy.Migrations;

[TestFixture]
[FixtureLifeCycle(LifeCycle.SingleInstance)]
public class WhatIfMigrationStateTests
{
    [Test]
    public void GetState_NullTarget()
    {
        var state = new WhatIfMigrationState();

        Should.Throw<ArgumentNullException>(() =>
        {
            state.GetState(null!, new("A"));
        });
    }

    [Test]
    public void GetState_NullMigration()
    {
        var state = new WhatIfMigrationState();

        Should.Throw<ArgumentNullException>(() =>
        {
            state.GetState(new("Server=.;Database=A"), null!);
        });
    }

    [Test]
    public void GetState_Initial()
    {
        var state     = new WhatIfMigrationState();
        var target    = new Target("Server=.;Database=A");
        var migration = new Migration("A") { State = MigrationState.AppliedPre };

        state.GetState(target, migration).ShouldBe(MigrationState.AppliedPre);
    }

    [Test]
    public void GetState_Applied()
    {
        var state     = new WhatIfMigrationState();
        var target    = new Target("Server=.;Database=A");
        var migration = new Migration("A");

        state.GetState(target, migration).ShouldBe(MigrationState.NotApplied);

        state.OnApplied(target, migration, MigrationPhase.Pre);
        state.GetState(target, migration).ShouldBe(MigrationState.AppliedPre);

        state.OnApplied(target, migration, MigrationPhase.Core);
        state.GetState(target, migration).ShouldBe(MigrationState.AppliedCore);

        state.OnApplied(target, migration, MigrationPhase.Post);
        state.GetState(target, migration).ShouldBe(MigrationState.AppliedPost);
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
    public void OnApplied_AlreadyApplied()
    {
        var state     = new WhatIfMigrationState();
        var target    = new Target("Server=.;Database=A");
        var migration = new Migration("A") { State = MigrationState.AppliedCore };

        Should.Throw<ArgumentOutOfRangeException>(() =>
        {
            state.OnApplied(target, migration, MigrationPhase.Core);
        });
    }
}
