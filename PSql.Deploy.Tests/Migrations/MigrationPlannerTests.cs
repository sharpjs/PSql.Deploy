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

        var plan = new MigrationPlanner(new[] { a }).CreatePlan();

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

        var plan = new MigrationPlanner(new[] { a, b, c }).CreatePlan();

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
        var d = MakeMigration("d", depends: "b");
        var e = MakeMigration("e");

        var plan = new MigrationPlanner(new[] { a, b, c, d, e }).CreatePlan();

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
        var c = MakeMigration("c", NotApplied, "a");
        var d = MakeMigration("d", NotApplied, "a", "b");
        var e = MakeMigration("e");

        var plan = new MigrationPlanner(new[] { a, b, c, d, e }).CreatePlan();

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
        var c = MakeMigration("c", NotApplied, "a");
        var d = MakeMigration("d", AppliedCore, "a", "b");
        var e = MakeMigration("e", AppliedPost);

        var plan = new MigrationPlanner(new[] { a, b, c, d, e }).CreatePlan();

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
        var migration = MakeMigration("_Begin");
        migration.IsPseudo = true;
        return migration;
    }

    private static Migration MakeEndPseudoMigration()
    {
        var migration = MakeMigration("_End");
        migration.IsPseudo = true;
        return migration;
    }

    private static Migration MakeMigration(
        string          name,
        MigrationState  state = NotApplied,
        params string[] depends)
    {
        return new Migration
        {
            Name     = name,
            PreSql   = name + ":Pre",
            CoreSql  = name + ":Core",
            PostSql  = name + ":Post",
            Depends  = depends,
            State2   = state
        };
    }
}
