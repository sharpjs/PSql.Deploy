// Copyright Subatomix Research Inc.
// SPDX-License-Identifier: MIT

namespace PSql.Deploy.Seeds;

[TestFixture]
public class SeedLoaderTests
{
    [Test]
    public void Load_NullSeed()
    {
        Should.Throw<ArgumentNullException>(static () =>
        {
            SeedLoader.Load(null!);
        });
    }

    [Test]
    public void Load_MissingModuleName()
    {
        var seed = WithSeed("MissingModuleName");

        Should.Throw<FormatException>(() =>
        {
            SeedLoader.Load(seed);
        });
    }

    [Test]
    public void Load_Empty()
    {
        var seed = WithSeed("Empty");

        var loadedSeed = SeedLoader.Load(seed);

        loadedSeed.Seed   .ShouldBeSameAs(seed);
        loadedSeed.Modules.AssignTo(out var modules);

        modules            .ShouldHaveSingleItem();
        modules[0].Name    .ShouldBe("(init)");
        modules[0].Provides.ShouldBeEmpty();
        modules[0].Requires.ShouldBeEmpty();
        modules[0].Batches .ShouldBeEmpty();
    }

    [Test]
    public void Load_Typical()
    {
        var seed = WithSeed("Typical");

        var loadedSeed = SeedLoader.Load(seed, [("foo", "bar")]);

        loadedSeed.Seed   .ShouldBeSameAs(seed);
        loadedSeed.Modules.AssignTo(out var modules);

        modules.Length       .ShouldBe(3);
        modules[0].Name      .ShouldBe("(init)");
        modules[0].Provides  .ShouldBeEmpty();
        modules[0].Requires  .ShouldBeEmpty();
        modules[0].Batches   .ShouldHaveSingleItem();
        modules[0].Batches[0].ShouldBe("PRINT 'This is in the initial module.';" + Environment.NewLine);

        modules[1].Name      .ShouldBe("a");
        modules[1].Provides  .ShouldBe(ImmutableArray.Create("x", "y"));
        modules[1].Requires  .ShouldBeEmpty();
        modules[1].Batches   .ShouldHaveSingleItem();
        modules[1].Batches[0].ShouldBe(
            """
            --# PROVIDES: x y
            --# provides: y x
            --# Provides:
            PRINT 'This is in module a.';
            PRINT 'The value of ''foo'' is bar.';

            """
        );
        // TODO: I don't think the magic comment should be included in the batch text.

        modules[2].Name      .ShouldBe("b");
        modules[2].Provides  .ShouldBeEmpty();
        modules[2].Requires  .ShouldBe(ImmutableArray.Create("x", "y"));
        modules[2].Batches   .ShouldHaveSingleItem();
        modules[2].Batches[0].ShouldBe(
            """
            --# REQUIRES:  x  y
            --# requires:  y  x
            --# Requires:  
            PRINT 'This is in module b.';

            """
        );
    }

    private static Seed WithSeed(string name)
    {
        var path = Path.Combine(
            TestContext.CurrentContext.TestDirectory,
            "TestDbs", "A", "Seeds", name, "_Main.sql"
        );

        return new Seed(name, path);
    }
}
