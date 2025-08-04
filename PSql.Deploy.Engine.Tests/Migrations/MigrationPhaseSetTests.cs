// Copyright Subatomix Research Inc.
// SPDX-License-Identifier: MIT

namespace PSql.Deploy.Migrations;

using static MigrationPhase;

[TestFixture]
[FixtureLifeCycle(LifeCycle.SingleInstance)]
public class MigrationPhaseSetTests
{
    // To make truth tables easier to read
    const bool Yes = true, ___ = false;

    [Test]
    public void Default()
    {
        var set = default(MigrationPhaseSet);

        set.ShouldBe([]);
    }

    [Test]
    public void Construct_NotNull()
    {                                                 // an invalid phase
        var set = new MigrationPhaseSet([Post, Pre, (MigrationPhase) 13]);

        set.ShouldBe([Pre, Post]);
    }

    [Test]
    public void Construct_Null()
    {
        var set = new MigrationPhaseSet(null);

        set.ShouldBe([Pre, Core, Post]);
    }

    [Test]
    [TestCase( (MigrationPhase[]) [               ], 0 )]
    [TestCase( (MigrationPhase[]) [Pre            ], 1 )]
    [TestCase( (MigrationPhase[]) [     Core      ], 1 )]
    [TestCase( (MigrationPhase[]) [           Post], 1 )]
    [TestCase( (MigrationPhase[]) [Pre, Core      ], 2 )]
    [TestCase( (MigrationPhase[]) [Pre,       Post], 2 )]
    [TestCase( (MigrationPhase[]) [     Core, Post], 2 )]
    [TestCase( (MigrationPhase[]) [Pre, Core, Post], 3 )]
    public void Count(MigrationPhase[] phases, int expected)
    {
        new MigrationPhaseSet(phases).Count.ShouldBe(expected);
    }

    [Test]
    [TestCase( (MigrationPhase[]) [               ], ___, ___, ___ )]
    [TestCase( (MigrationPhase[]) [Pre            ], Yes, ___, ___ )]
    [TestCase( (MigrationPhase[]) [     Core      ], ___, Yes, ___ )]
    [TestCase( (MigrationPhase[]) [           Post], ___, ___, Yes )]
    [TestCase( (MigrationPhase[]) [Pre, Core      ], Yes, Yes, ___ )]
    [TestCase( (MigrationPhase[]) [Pre,       Post], Yes, ___, Yes )]
    [TestCase( (MigrationPhase[]) [     Core, Post], ___, Yes, Yes )]
    [TestCase( (MigrationPhase[]) [Pre, Core, Post], Yes, Yes, Yes )]
    public void Contains(MigrationPhase[] phases, bool pre, bool core, bool post)
    {
        var set = new MigrationPhaseSet(phases);

        set.Contains(Pre ).ShouldBe(pre);
        set.Contains(Core).ShouldBe(core);
        set.Contains(Post).ShouldBe(post);
    }

    [Test]
    [TestCase( (MigrationPhase[]) [               ], (MigrationPhase) 32 )] // invalid
    [TestCase( (MigrationPhase[]) [Pre            ], Pre  )]
    [TestCase( (MigrationPhase[]) [     Core      ], Core )]
    [TestCase( (MigrationPhase[]) [           Post], Post )]
    [TestCase( (MigrationPhase[]) [Pre, Core      ], Pre  )]
    [TestCase( (MigrationPhase[]) [Pre,       Post], Pre  )]
    [TestCase( (MigrationPhase[]) [     Core, Post], Core )]
    [TestCase( (MigrationPhase[]) [Pre, Core, Post], Pre  )]
    public void First(MigrationPhase[] phases, MigrationPhase expected)
    {
        new MigrationPhaseSet(phases).First().ShouldBe(expected);
    }

    [Test]
    public void GetEnumerator()
    {
        var set = new MigrationPhaseSet([Post, Pre]);

        using var enumerator = set.GetEnumerator();

        enumerator.MoveNext().ShouldBe(true); enumerator.Current.ShouldBe(Pre);
        enumerator.MoveNext().ShouldBe(true); enumerator.Current.ShouldBe(Post);
        enumerator.MoveNext().ShouldBe(false);

        enumerator.Reset();

        enumerator.MoveNext().ShouldBe(true); enumerator.Current.ShouldBe(Pre);
        enumerator.MoveNext().ShouldBe(true); enumerator.Current.ShouldBe(Post);
        enumerator.MoveNext().ShouldBe(false);
    }

    [Test]
    public void GetEnumerator_Generic()
    {
        var set = new MigrationPhaseSet([Post, Pre]);

        using var enumerator = ((IEnumerable<MigrationPhase>) set).GetEnumerator();

        enumerator.MoveNext().ShouldBe(true); enumerator.Current.ShouldBe(Pre);
        enumerator.MoveNext().ShouldBe(true); enumerator.Current.ShouldBe(Post);
        enumerator.MoveNext().ShouldBe(false);

        enumerator.Reset();

        enumerator.MoveNext().ShouldBe(true); enumerator.Current.ShouldBe(Pre);
        enumerator.MoveNext().ShouldBe(true); enumerator.Current.ShouldBe(Post);
        enumerator.MoveNext().ShouldBe(false);
    }

    [Test]
    public void GetEnumerator_NonGeneric()
    {
        var set = new MigrationPhaseSet([Post, Pre]);

        var enumerator = ((IEnumerable) set).GetEnumerator();

        enumerator.MoveNext().ShouldBe(true); enumerator.Current.ShouldBe(Pre);
        enumerator.MoveNext().ShouldBe(true); enumerator.Current.ShouldBe(Post);
        enumerator.MoveNext().ShouldBe(false);

        enumerator.Reset();

        enumerator.MoveNext().ShouldBe(true); enumerator.Current.ShouldBe(Pre);
        enumerator.MoveNext().ShouldBe(true); enumerator.Current.ShouldBe(Post);
        enumerator.MoveNext().ShouldBe(false);
    }
}
