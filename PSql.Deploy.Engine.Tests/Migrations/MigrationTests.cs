// Copyright Subatomix Research Inc.
// SPDX-License-Identifier: MIT

namespace PSql.Deploy.Migrations;

[TestFixture]
public class MigrationTests
{
    [Test]
    public void Construct_NullName()
    {
        Should.Throw<ArgumentNullException>(() => new Migration(null!));
    }

    [Test]
    public void Construct_EmptyName()
    {
        Should.Throw<ArgumentException>(() => new Migration(""));
    }

    [Test]
    public void Name_Get()
    {
        new Migration("m").Name.ShouldBe("m");
    }

    [Test]
    [TestCase("Normal", false)]
    [TestCase("_Begin", true )]
    [TestCase("_End",   true )]
    public void IsPseudo(string name, bool expected)
    {
        new Migration(name).IsPseudo.ShouldBe(expected);
    }

    [Test]
    public void Path_Get()
    {
        new Migration("m").Path.ShouldBeNull();
    }

    [Test]
    public void Path_Set()
    {
        new Migration("m") { Path = "p" }.Path.ShouldBe("p");
    }

    [Test]
    public void Hash_Get()
    {
        new Migration("m").Hash.ShouldBeEmpty();
    }

    [Test]
    public void Hash_Set()
    {
        new Migration("m") { Hash = "h" }.Hash.ShouldBe("h");
    }

    [Test]
    public void State_Get()
    {
        new Migration("m").State.ShouldBe(MigrationState.NotApplied);
    }

    [Test]
    public void State_Set()
    {
        new Migration("m") { State = MigrationState.AppliedPre }
            .State.ShouldBe(MigrationState.AppliedPre);
    }

    [Test]
    public void HasChanged_Get()
    {
        new Migration("m").HasChanged.ShouldBeFalse();
    }

    [Test]
    public void HasChanged_Set()
    {
        new Migration("m") { HasChanged = true }.HasChanged.ShouldBeTrue();
    }

    [Test]
    public void IsContentLoaded_Get()
    {
        new Migration("m").IsContentLoaded.ShouldBeFalse();
    }

    [Test]
    public void IsContentLoaded_Set()
    {
        new Migration("m") { IsContentLoaded = true }.IsContentLoaded.ShouldBeTrue();
    }

    [Test]
    public void Pre_Get()
    {
        new Migration("m").Pre.ShouldNotBeNull();
    }

    [Test]
    public void Core_Get()
    {
        new Migration("m").Core.ShouldNotBeNull();
    }

    [Test]
    public void Post_Get()
    {
        new Migration("m").Post.ShouldNotBeNull();
    }

    [Test]
    public void DependsOn_Get()
    {
        new Migration("m").DependsOn.ShouldBeEmpty();
    }

    [Test]
    public void DependsOn_Set()
    {
        var value = ImmutableArray.Create<MigrationReference>(new("a"), new("b"));

        new Migration("m") { DependsOn = value }.DependsOn.ShouldBe(value);
    }

    [Test]
    public void Diagnostics_Get()
    {
        new Migration("m").Diagnostics.ShouldBeEmpty();
    }

    [Test]
    public void Diagnostics_Set()
    {
        var value = ImmutableArray.Create<MigrationDiagnostic>(
            new(isError: false, "a"),
            new(isError: true,  "b")
        );

        new Migration("m") { Diagnostics = value }.Diagnostics.ShouldBe(value);
    }

    [Test]
    public void This_Get()
    {
        var migration = new Migration("m");

        migration[MigrationPhase.Pre ].ShouldBeSameAs(migration.Pre );
        migration[MigrationPhase.Core].ShouldBeSameAs(migration.Core);
        migration[MigrationPhase.Post].ShouldBeSameAs(migration.Post);
    }

    [Test]
    public void This_Get_OutOfRange()
    {
        Should.Throw<ArgumentOutOfRangeException>(static () =>
        {
            _ = new Migration("m")[(MigrationPhase) (-1)];
        });
    }

    [Test]
    [TestCase(MigrationState.NotApplied,  null)]
    [TestCase(MigrationState.AppliedPre,  MigrationPhase.Pre )]
    [TestCase(MigrationState.AppliedCore, MigrationPhase.Core)]
    [TestCase(MigrationState.AppliedPost, MigrationPhase.Post)]
    public void LatestAppliedPhase_Get(MigrationState state, MigrationPhase? expected)
    {
        new Migration("m") { State = state }.LatestAppliedPhase.ShouldBe(expected);
    }

    [Test]
    [TestCase(MigrationState.NotApplied,  MigrationPhase.Pre, false)]
    [TestCase(MigrationState.AppliedPre,  MigrationPhase.Pre, true )]
    [TestCase(MigrationState.AppliedCore, MigrationPhase.Pre, true )]
    public void LatestAppliedPhase(MigrationState state, MigrationPhase phase, bool expected)
    {
        new Migration("m") { State = state }.IsAppliedThrough(phase).ShouldBe(expected);
    }

    [Test]
    public void ToStringMethod()
    {
        new Migration("m") { State = MigrationState.AppliedPre }.ToString().ShouldBe("m");
    }
}
