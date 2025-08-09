// Copyright Subatomix Research Inc.
// SPDX-License-Identifier: MIT

namespace PSql.Deploy.Seeds;

[TestFixture]
public class LoadedSeedTests
{
    [Test]
    public void Construct_NullSeed()
    {
        Should.Throw<ArgumentNullException>(() =>
        {
            _ = new LoadedSeed(null!, default);
        });
    }

    [Test]
    public void Seed_Get()
    {
        var seed = new Seed("a", "b");

        var loadedSeed = new LoadedSeed(seed, ImmutableArray<SeedModule>.Empty);

        loadedSeed.Seed.ShouldBeSameAs(seed);
    }

    [Test]
    public void Modules_Get()
    {
        var seed    = new Seed("a", "b");
        var modules = ImmutableArray.Create(new SeedModule("c", false, default, default, default));

        var loadedSeed = new LoadedSeed(seed, modules);

        loadedSeed.Modules.ShouldBe(modules);
    }
}
