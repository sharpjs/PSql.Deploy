// Copyright 2023 Subatomix Research Inc.
// SPDX-License-Identifier: ISC

namespace PSql.Deploy.Migrations;

using static MigrationPhase;
using static MigrationState;

[TestFixture]
public class MigrationPlannerTests
{
    [Test]
    public void SingleMigration()
    {
        var a = MakeMigration("a");

        var plan = MakePlanner(a).CreatePlan();

        plan     .Should().NotBeNull();
        plan.Pre .Should().Equal(a);
        plan.Core.Should().Equal((a, Core));
        plan.Post.Should().Equal(a);
    }

    [Test]
    public void MultipleMigrations()
    {
        var a = MakeBeginPseudoMigration();
        var b = MakeMigration("b");
        var c = MakeEndPseudoMigration();

        var plan = MakePlanner(a, b, c).CreatePlan();

        plan     .Should().NotBeNull();
        plan.Pre .Should().Equal(a, b, c);
        plan.Core.Should().Equal((a, Core), (b, Core), (c, Core));
        plan.Post.Should().Equal(a, b, c);
    }

    [Test]
    public void SingleDependency()
    {
        var a = MakeMigration("a");
        var b = MakeMigration("b");
        var c = MakeMigration("c");
        var d = MakeMigration("d", NotApplied, b);
        var e = MakeMigration("e");

        var plan = MakePlanner(a, b, c, d, e).CreatePlan();

        plan     .Should().NotBeNull();
        plan.Pre .Should().Equal(a, b, c);
        plan.Core.Should().Equal(
            // Core                             // Early Post           // Late Pre
            (a, Core), (b, Core), (c, Core),    (a, Post), (b, Post),   (d, Pre), (e, Pre),
            (d, Core), (e, Core)
        );
        plan.Post.Should().Equal(c, d, e);
    }

    [Test]
    public void MultipleDependencies()
    {
        var a = MakeMigration("a");
        var b = MakeMigration("b");
        var c = MakeMigration("c", NotApplied, a);
        var d = MakeMigration("d", NotApplied, a, b);
        var e = MakeMigration("e");

        var plan = MakePlanner(a, b, c, d, e).CreatePlan();

        plan     .Should().NotBeNull();
        plan.Pre .Should().Equal(a, b);
        plan.Core.Should().Equal(
            // Core                 // Early Post   // Late Pre
            (a, Core), (b, Core),   (a, Post),      (c, Pre),
            (c, Core),              (b, Post),      (d, Pre), (e, Pre),
            (d, Core), (e, Core)
        );
        plan.Post.Should().Equal(c, d, e);
    }

    [Test]
    public void MultipleDependencies_PartiallyApplied()
    {
        var a = MakeMigration("a", AppliedPre);
        var b = MakeMigration("b", AppliedPost);
        var c = MakeMigration("c", NotApplied,  a);
        var d = MakeMigration("d", AppliedCore, a, b);
        var e = MakeMigration("e", AppliedPost);

        var plan = MakePlanner(a, b, c, d, e).CreatePlan();

        plan     .Should().NotBeNull();
        plan.Pre .Should().BeEmpty();
        plan.Core.Should().Equal(
            // Core         // Early Post   // Late Pre
            (a, Core),      (a, Post),      (c, Pre),
            (c, Core)
        );
        plan.Post.Should().Equal(c, d);
    }

    private static Migration MakeBeginPseudoMigration()
    {
        return MakeMigration(Migration.BeginPseudoMigrationName);
    }

    private static Migration MakeEndPseudoMigration()
    {
        return MakeMigration(Migration.EndPseudoMigrationName);
    }

    private static Migration MakeMigration(
        string             name,
        MigrationState     state = NotApplied,
        params Migration[] dependsOn)
    {
        return new Migration(name)
        {
            State     = state,
            Pre       = { Sql = name + ":Pre"  },
            Core      = { Sql = name + ":Core" },
            Post      = { Sql = name + ":Post" },
            DependsOn = ImmutableArray.CreateRange(dependsOn.ToImmutableArray(), ToReference)
        };
    }

    private static MigrationReference ToReference(Migration migration)
    {
        return new(migration.Name) { Migration = migration };
    }

    private static MigrationPlanner MakePlanner(params Migration[] migrations)
    {
        return new MigrationPlanner(ImmutableArray.Create(migrations));
    }
}
