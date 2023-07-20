// Copyright 2023 Subatomix Research Inc.
// SPDX-License-Identifier: ISC

namespace PSql.Deploy.Migrations;

[TestFixture]
public class MigrationTests
{
    [Test]
    public void Construct_NullName()
    {
        Invoking(() => new Migration(null!))
            .Should().Throw<ArgumentNullException>();
    }

    [Test]
    public void Construct_EmptyName()
    {
        Invoking(() => new Migration(""))
            .Should().ThrowExactly<ArgumentException>();
    }

    [Test]
    public void Name_Get()
    {
        new Migration("m").Name.Should().Be("m");
    }

    [Test]
    [TestCase("Normal", false)]
    [TestCase("_Begin", true )]
    [TestCase("_End",   true )]
    public void IsPseudo(string name, bool expected)
    {
        new Migration(name).IsPseudo.Should().Be(expected);
    }

    [Test]
    public void Path_Get()
    {
        new Migration("m").Path.Should().BeNull();
    }

    [Test]
    public void Path_Set()
    {
        new Migration("m") { Path = "p" }.Path.Should().Be("p");
    }

    [Test]
    public void Hash_Get()
    {
        new Migration("m").Hash.Should().BeEmpty();
    }

    [Test]
    public void Hash_Set()
    {
        new Migration("m") { Hash = "h" }.Hash.Should().Be("h");
    }

    [Test]
    public void State_Get()
    {
        new Migration("m").State.Should().Be(MigrationState.NotApplied);
    }

    [Test]
    public void State_Set()
    {
        new Migration("m") { State = MigrationState.AppliedPre }
            .State.Should().Be(MigrationState.AppliedPre);
    }

    [Test]
    public void HasChanged_Get()
    {
        new Migration("m").HasChanged.Should().BeFalse();
    }

    [Test]
    public void HasChanged_Set()
    {
        new Migration("m") { HasChanged = true }.HasChanged.Should().BeTrue();
    }

    [Test]
    public void IsContentLoaded_Get()
    {
        new Migration("m").IsContentLoaded.Should().BeFalse();
    }

    [Test]
    public void IsContentLoaded_Set()
    {
        new Migration("m") { IsContentLoaded = true }.IsContentLoaded.Should().BeTrue();
    }

    [Test]
    public void Pre_Get()
    {
        new Migration("m").Pre.Should().NotBeNull();
    }

    [Test]
    public void Core_Get()
    {
        new Migration("m").Core.Should().NotBeNull();
    }

    [Test]
    public void Post_Get()
    {
        new Migration("m").Post.Should().NotBeNull();
    }

    [Test]
    public void DependsOn_Get()
    {
        new Migration("m").DependsOn.Should().BeEmpty();
    }

    [Test]
    public void DependsOn_Set()
    {
        var value = ImmutableArray.Create<MigrationReference>(new("a"), new("b"));

        new Migration("m") { DependsOn = value }.DependsOn.Should().Equal(value);
    }

    [Test]
    public void Diagnostics_Get()
    {
        new Migration("m").Diagnostics.Should().BeEmpty();
    }

    [Test]
    public void Diagnostics_Set()
    {
        var value = ImmutableArray.Create<MigrationDiagnostic>(
            new(isError: false, "a"),
            new(isError: true,  "b")
        );

        new Migration("m") { Diagnostics = value }.Diagnostics.Should().Equal(value);
    }

    [Test]
    public void This_Get()
    {
        var migration = new Migration("m");

        migration[MigrationPhase.Pre ].Should().BeSameAs(migration.Pre );
        migration[MigrationPhase.Core].Should().BeSameAs(migration.Core);
        migration[MigrationPhase.Post].Should().BeSameAs(migration.Post);
    }

    [Test]
    public void This_Get_OutOfRange()
    {
        var migration = new Migration("m");

        migration
            .Invoking(m => m[(MigrationPhase) (-1)])
            .Should().Throw<ArgumentOutOfRangeException>();
    }

    [Test]
    [TestCase(MigrationState.NotApplied,  null)]
    [TestCase(MigrationState.AppliedPre,  MigrationPhase.Pre )]
    [TestCase(MigrationState.AppliedCore, MigrationPhase.Core)]
    [TestCase(MigrationState.AppliedPost, MigrationPhase.Post)]
    public void LatestAppliedPhase_Get(MigrationState state, MigrationPhase? expected)
    {
        new Migration("m") { State = state }.LatestAppliedPhase.Should().Be(expected);
    }

    [Test]
    [TestCase(MigrationState.NotApplied,  MigrationPhase.Pre, false)]
    [TestCase(MigrationState.AppliedPre,  MigrationPhase.Pre, true )]
    [TestCase(MigrationState.AppliedCore, MigrationPhase.Pre, true )]
    public void LatestAppliedPhase(MigrationState state, MigrationPhase phase, bool expected)
    {
        new Migration("m") { State = state }.IsAppliedThrough(phase).Should().Be(expected);
    }

    [Test]
    public void ToStringMethod()
    {
        new Migration("m") { State = MigrationState.AppliedPre }.ToString().Should().Be("m");
    }
}
