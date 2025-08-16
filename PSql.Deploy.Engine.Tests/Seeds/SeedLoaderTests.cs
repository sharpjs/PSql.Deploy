// Copyright Subatomix Research Inc.
// SPDX-License-Identifier: MIT

namespace PSql.Deploy.Seeds;

[TestFixture]
public class SeedLoaderTests
{
    // PSql.Deploy tests use CRLF line endings in SQL files so that file
    // content is stable across platforms.
    private const string Eol = "\r\n";

    [Test]
    public void Load_NullSeed()
    {
        Should.Throw<ArgumentNullException>(static () =>
        {
            SeedLoader.Load(null!);
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
        modules[0].Name    .ShouldBe("init");
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

        modules.Length       .ShouldBe(4);

        modules[0].Name      .ShouldBe("init");
        modules[0].WorkerId  .ShouldBe(0);
        modules[0].Provides  .ShouldBeEmpty();
        modules[0].Requires  .ShouldBeEmpty();
        modules[0].Batches   .ShouldHaveSingleItem();
        modules[0].Batches[0].ShouldBe(""
            + "PRINT 'This is in the initial module.';" + Eol
        );

        modules[1].Name      .ShouldBe("init-worker");
        modules[1].WorkerId  .ShouldBe(-1);
        modules[1].Provides  .ShouldBeEmpty();
        modules[1].Requires  .ShouldHaveSingleItem().ShouldBe("init");
        modules[1].Batches   .ShouldHaveSingleItem();
        modules[1].Batches[0].ShouldBe(""
            + "--# WORKER: all"                           + Eol
            + "PRINT 'This is in an all-worker module.';" + Eol
        );

        modules[2].Name      .ShouldBe("a");
        modules[2].WorkerId  .ShouldBe(0);
        modules[2].Provides  .ShouldBe(ImmutableArray.Create("x", "y", "z"));
        modules[2].Requires  .ShouldHaveSingleItem().ShouldBe("init");
        modules[2].Batches   .ShouldHaveSingleItem();
        modules[2].Batches[0].ShouldBe(""
            + "--# PROVIDES: x y"                       + Eol
            + "--# provides: y x"                       + Eol
            + "--# Provides:"                           + Eol
            + "PRINT 'This is in module a.';"           + Eol
            + "PRINT 'The value of ''foo'' is bar.';"   + Eol
            + "-- The value of foo is bar."             + Eol
        );
        // TODO: I don't think the magic comment should be included in the batch text.

        modules[3].Name      .ShouldBe("b");
        modules[3].WorkerId  .ShouldBe(0);
        modules[3].Provides  .ShouldBeEmpty();
        modules[3].Requires  .ShouldBe(ImmutableArray.Create("init", "x", "y", "z"));
        modules[3].Batches   .ShouldHaveSingleItem();
        modules[3].Batches[0].ShouldBe(""
            + "--# REQUIRES:  x  y  z"                  + Eol
            + "--# requires:  z  y  x"                  + Eol
            + "--# Requires:  "                         + Eol
            + "PRINT 'This is in module b.';"           + Eol
        );
    }

    [Test]
    public void Load_ModuleNameMissing()
    {
        var seed = WithSeed("MissingModuleName");

        Should.Throw<FormatException>(() =>
        {
            SeedLoader.Load(seed);
        });
    }

    [Test]
    public void Load_ProvidesInit()
    {
        var seed   = WithSeed("ProvidesInit");
        var loaded = SeedLoader.Load(seed);

        loaded.Seed   .ShouldBeSameAs(seed);
        loaded.Modules.AssignTo(out var modules);

        modules.Length.ShouldBe(2);

        modules[0].Name      .ShouldBe("init");
        modules[0].WorkerId  .ShouldBe(0);
        modules[0].Provides  .ShouldBeEmpty();
        modules[0].Requires  .ShouldBe(ImmutableArray.Create("pre-init"));
        modules[0].Batches   .ShouldHaveSingleItem();
        modules[0].Batches[0].ShouldBe(
            "--# REQUIRES: pre-init" + Eol
        );

        modules[1].Name      .ShouldBe("pre-init");
        modules[1].WorkerId  .ShouldBe(0);
        modules[1].Provides  .ShouldBe(ImmutableArray.Create("init"));
        modules[1].Requires  .ShouldBeEmpty();
        modules[1].Batches   .ShouldHaveSingleItem();
        modules[1].Batches[0].ShouldBe(
            "--# PROVIDES: init" + Eol
        );
    }

    [Test]
    public void Load_WorkerAll()
    {
        var seed   = WithSeed("WorkerAll");
        var loaded = SeedLoader.Load(seed);

        loaded.Seed   .ShouldBeSameAs(seed);
        loaded.Modules.AssignTo(out var modules);

        modules.Length     .ShouldBe(2);

        modules[0].Name    .ShouldBe("init");
        modules[0].WorkerId.ShouldBe(0);
        modules[0].Provides.ShouldBeEmpty();
        modules[0].Requires.ShouldBeEmpty();
        modules[0].Batches .ShouldBeEmpty();

        modules[1].Name    .ShouldBe("init-worker");
        modules[1].WorkerId.ShouldBe(-1);
        modules[1].Provides.ShouldBeEmpty();
        modules[1].Requires.ShouldHaveSingleItem().ShouldBe("init");
        modules[1].Batches .ShouldHaveSingleItem();
        modules[1].Batches[0].ShouldBe("--# WORKER: all" + Eol);
    }

    [Test]
    public void Load_WorkerAny()
    {
        var seed   = WithSeed("WorkerAny");
        var loaded = SeedLoader.Load(seed);

        loaded.Seed   .ShouldBeSameAs(seed);
        loaded.Modules.AssignTo(out var modules);

        modules.Length.ShouldBe(1);

        modules[0].Name      .ShouldBe("init");
        modules[0].WorkerId  .ShouldBe(0);
        modules[0].Provides  .ShouldBeEmpty();
        modules[0].Requires  .ShouldBeEmpty();
        modules[0].Batches   .ShouldHaveSingleItem();
        modules[0].Batches[0].ShouldBe(
            "--# worker:  ANY" + Eol
        );
    }

    [Test]
    public void Load_WorkerMultipleArgs()
    {
        var path = WithSeed("WorkerMultipleArgs");

        Should.Throw<FormatException>(() =>
        {
            SeedLoader.Load(path);
        })
        .Message.ShouldBe(
            "The WORKER magic comment expects exactly one argument."
        );
    }

    [Test]
    public void Load_WorkerInvalid()
    {
        var seed = WithSeed("WorkerInvalid");

        Should.Throw<FormatException>(() =>
        {
            SeedLoader.Load(seed);
        })
        .Message.ShouldBe(
            "The WORKER magic comment argument must be 'all' or 'any', " +
            "case-insensitive, without quotes."
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
