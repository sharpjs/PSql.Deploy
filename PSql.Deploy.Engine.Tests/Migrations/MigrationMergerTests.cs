// Copyright Subatomix Research Inc.
// SPDX-License-Identifier: MIT

namespace PSql.Deploy.Migrations;

[TestFixture]
public class MigrationMergerTests : TestHarnessBase
{
    private readonly Mock<IMigrationSessionInternal> _session;

    public MigrationMergerTests()
    {
        _session = Mocks.Create<IMigrationSessionInternal>();
    }

    [Test]
    public void Merge_Empty()
    {
        new MigrationMerger(_session.Object)
            .Merge([], [])
            .ShouldBeEmpty();
    }

    [Test]
    public void Merge_DefinedWithoutApplied()
    {
        var defined = new Migration("m");

        _session
            .Setup(i => i.LoadContent(defined))
            .Verifiable();

        new MigrationMerger(_session.Object)
            .Merge([defined], [])
            .ShouldBe(ImmutableArray.Create(defined));
    }

    [Test]
    public void Merge_AppliedWithoutDefined_FullyApplied()
    {
        var applied = new Migration("m") { State = MigrationState.AppliedPost };

        new MigrationMerger(_session.Object)
            .Merge([], [applied])
            .ShouldBeEmpty();
    }

    [Test]
    public void Merge_AppliedWithoutDefined_PartiallyAplied()
    {
        var applied = new Migration("m") { State = MigrationState.AppliedCore };

        new MigrationMerger(_session.Object)
            .Merge([], [applied])
            .ShouldBe(ImmutableArray.Create(applied));
    }

    [Test]
    public void Merge_DefinedAndApplied_FullyApplied()
    {
        var defined = new Migration("m") { Hash = "h", Path = "p" };
        var applied = new Migration("m") { Hash = "h", State = MigrationState.AppliedPost };

        new MigrationMerger(_session.Object)
            .Merge([defined], [applied])
            .ShouldBe(ImmutableArray.Create(applied));

        applied.Path      .ShouldBe(defined.Path);
        applied.Hash      .ShouldBe(defined.Hash);
        applied.HasChanged.ShouldBeFalse();
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

        _session
            .Setup(i => i.LoadContent(defined))
            .Verifiable();

        new MigrationMerger(_session.Object)
            .Merge([defined], [applied])
            .ShouldBe(ImmutableArray.Create(applied));

        applied.Path           .ShouldBe(defined.Path);
        applied.Hash           .ShouldBe(defined.Hash);
        applied.HasChanged     .ShouldBeFalse();
        applied.Pre .Sql       .ShouldBe(defined.Pre .Sql);
        applied.Core.Sql       .ShouldBe(defined.Core.Sql);
        applied.Post.Sql       .ShouldBe(defined.Post.Sql);
        applied.Pre .IsRequired.ShouldBe(defined.Pre .IsRequired);
        applied.Core.IsRequired.ShouldBe(defined.Core.IsRequired);
        applied.Post.IsRequired.ShouldBe(defined.Post.IsRequired);
        applied.DependsOn      .ShouldBe(defined.DependsOn);
        applied.IsContentLoaded.ShouldBe(defined.IsContentLoaded);
    }

    [Test]
    public void Merge_DefinedAndApplied_Changed()
    {
        var defined = new Migration("m") { Hash = "h0", Path = "p" };
        var applied = new Migration("m") { Hash = "h1", State = MigrationState.AppliedPost };

        new MigrationMerger(_session.Object)
            .Merge([defined], [applied])
            .ShouldBe(ImmutableArray.Create(applied));

        applied.Path      .ShouldBe(defined.Path);
        applied.Hash      .ShouldBe(defined.Hash);
        applied.HasChanged.ShouldBeTrue();
    }

    [Test]
    public void Merge_DefinedAndApplied_NoHashInApplied()
    {
        var defined = new Migration("m") { Hash = "h", Path = "p" };
        var applied = new Migration("m") {             State = MigrationState.AppliedPost };

        new MigrationMerger(_session.Object)
            .Merge([defined], [applied])
            .ShouldBe(ImmutableArray.Create(applied));

        applied.Path      .ShouldBe(defined.Path);
        applied.Hash      .ShouldBe(defined.Hash);
        applied.HasChanged.ShouldBeFalse();
    }
}
