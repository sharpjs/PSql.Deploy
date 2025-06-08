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
            _ = new SeedModule(null!, default, default, default);
        });
    }

    [Test]
    public void Name_Get()
    {
        var module = new SeedModule("a", default, default, default);

        module.Name.ShouldBe("a");
    }

    [Test]
    public void Batches_Get()
    {
        var items = ImmutableArray.Create("b");

        var module = new SeedModule("a", items, default, default);

        module.Batches.ShouldBe(items);
    }

    [Test]
    public void Provides_Get()
    {
        var items = ImmutableArray.Create("b");

        var module = new SeedModule("a", default, items, default);

        module.Provides.ShouldBe(items);
    }

    [Test]
    public void Requires_Get()
    {
        var items = ImmutableArray.Create("b");

        var module = new SeedModule("a", default, default, items);

        module.Requires.ShouldBe(items);
    }
}
