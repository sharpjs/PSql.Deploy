// Copyright Subatomix Research Inc.
// SPDX-License-Identifier: MIT

namespace PSql.Deploy.Seeds;

[TestFixture]
public class SeedModuleTests
{
    [Test]
    public void Construct_NullName()
    {
        Should.Throw<ArgumentNullException>(() =>
        {
            _ = new SeedModule(null!, false, default, default, default);
        });
    }

    [Test]
    public void Name_Get()
    {
        var module = new SeedModule("a", false, default, default, default);

        module.Name.ShouldBe("a");
    }

    [Test]
    [TestCase(false)]
    [TestCase(true )]
    public void AllWorkers_Get(bool value)
    {
        var module = new SeedModule("a", value, default, default, default);

        module.AllWorkers.ShouldBe(value);
    }

    [Test]
    public void Batches_Get()
    {
        var items = ImmutableArray.Create("b");

        var module = new SeedModule("a", false, items, default, default);

        module.Batches.ShouldBe(items);
    }

    [Test]
    public void Provides_Get()
    {
        var items = ImmutableArray.Create("b");

        var module = new SeedModule("a", false, default, items, default);

        module.Provides.ShouldBe(items);
    }

    [Test]
    public void Requires_Get()
    {
        var items = ImmutableArray.Create("b");

        var module = new SeedModule("a", false, default, default, items);

        module.Requires.ShouldBe(items);
    }
}
