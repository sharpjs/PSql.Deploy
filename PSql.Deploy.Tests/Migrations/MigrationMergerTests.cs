// Copyright 2024 Subatomix Research Inc.
// SPDX-License-Identifier: ISC

namespace PSql.Deploy.Migrations;

[TestFixture]
[FixtureLifeCycle(LifeCycle.InstancePerTestCase)]
public class MigrationMergerTests : TestHarnessBase
{
    private readonly Mock<IMigrationInternals> _internals;

    public MigrationMergerTests()
    {
        _internals = Mocks.Create<IMigrationInternals>();
    }

    [Test]
    public void Invoker()
    {
        new MigrationMerger(_internals.Object).Internals.Should().BeSameAs(_internals.Object);
    }

    [Test]
    public void Merge_Empty()
    {
        new MigrationMerger(_internals.Object)
            .Merge(
                definedMigrations: new Migration[0],
                appliedMigrations: new Migration[0]
            )
            .Should().BeEmpty();
    }

    [Test]
    public void Merge_DefinedWithoutApplied()
    {
        var defined = new Migration("m");

        _internals
            .Setup(i => i.LoadContent(defined))
            .Verifiable();

        new MigrationMerger(_internals.Object)
            .Merge(
                definedMigrations: new[] { defined },
                appliedMigrations: new Migration[0]
            )
            .Should().Equal(defined);
    }

    [Test]
    public void Merge_AppliedWithoutDefined_FullyApplied()
    {
        var applied = new Migration("m") { State = MigrationState.AppliedPost };

        new MigrationMerger(_internals.Object)
            .Merge(
                definedMigrations: new Migration[0],
                appliedMigrations: new[] { applied }
            )
            .Should().BeEmpty();
    }

    [Test]
    public void Merge_AppliedWithoutDefined_PartiallyAplied()
    {
        var applied = new Migration("m") { State = MigrationState.AppliedCore };

        new MigrationMerger(_internals.Object)
            .Merge(
                definedMigrations: new Migration[0],
                appliedMigrations: new[] { applied }
            )
            .Should().Equal(applied);
    }

    [Test]
    public void Merge_DefinedAndApplied_FullyApplied()
    {
        var defined = new Migration("m") { Hash = "h", Path = "p" };
        var applied = new Migration("m") { Hash = "h", State = MigrationState.AppliedPost };

        new MigrationMerger(_internals.Object)
            .Merge(
                definedMigrations: new[] { defined },
                appliedMigrations: new[] { applied }
            )
            .Should().Equal(applied);

        applied.Path      .Should().Be(defined.Path);
        applied.Hash      .Should().Be(defined.Hash);
        applied.HasChanged.Should().BeFalse();
    }

    [Test]
    public void Merge_DefinedAndApplied_PartiallyApplied()
    {
        var defined = new Migration("m")
        {
            Hash            = "h",
            Path            = "p",
            Pre             = { Sql = "pre",  IsRequired = true  },
            Core            = { Sql = "core", IsRequired = false },
            Post            = { Sql = "post", IsRequired = true  },
            DependsOn       = ImmutableArray.Create(new MigrationReference("a")),
            IsContentLoaded = true,
        };

        var applied = new Migration("m")
        {
            Hash  = "h",
            State = MigrationState.AppliedCore
        };

        _internals
            .Setup(i => i.LoadContent(defined))
            .Verifiable();

        new MigrationMerger(_internals.Object)
            .Merge(
                definedMigrations: new[] { defined },
                appliedMigrations: new[] { applied }
            )
            .Should().Equal(applied);

        applied.Path           .Should().Be(defined.Path);
        applied.Hash           .Should().Be(defined.Hash);
        applied.HasChanged     .Should().BeFalse();
        applied.Pre .Sql       .Should().Be(defined.Pre .Sql);
        applied.Core.Sql       .Should().Be(defined.Core.Sql);
        applied.Post.Sql       .Should().Be(defined.Post.Sql);
        applied.Pre .IsRequired.Should().Be(defined.Pre .IsRequired);
        applied.Core.IsRequired.Should().Be(defined.Core.IsRequired);
        applied.Post.IsRequired.Should().Be(defined.Post.IsRequired);
        applied.DependsOn      .Should().Equal(defined.DependsOn);
        applied.IsContentLoaded.Should().Be(defined.IsContentLoaded);
    }

    [Test]
    public void Merge_DefinedAndApplied_Changed()
    {
        var defined = new Migration("m") { Hash = "h0", Path = "p" };
        var applied = new Migration("m") { Hash = "h1", State = MigrationState.AppliedPost };

        new MigrationMerger(_internals.Object)
            .Merge(
                definedMigrations: new[] { defined },
                appliedMigrations: new[] { applied }
            )
            .Should().Equal(applied);

        applied.Path      .Should().Be(defined.Path);
        applied.Hash      .Should().Be(defined.Hash);
        applied.HasChanged.Should().BeTrue();
    }

    [Test]
    public void Merge_DefinedAndApplied_NoHashInApplied()
    {
        var defined = new Migration("m") { Hash = "h", Path = "p" };
        var applied = new Migration("m") {             State = MigrationState.AppliedPost };

        new MigrationMerger(_internals.Object)
            .Merge(
                definedMigrations: new[] { defined },
                appliedMigrations: new[] { applied }
            )
            .Should().Equal(applied);

        applied.Path      .Should().Be(defined.Path);
        applied.Hash      .Should().Be(defined.Hash);
        applied.HasChanged.Should().BeFalse();
    }
}
