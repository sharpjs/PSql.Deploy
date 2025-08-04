// Copyright Subatomix Research Inc.
// SPDX-License-Identifier: MIT

namespace PSql.Deploy.Seeds;

[TestFixture]
public class SeedTests
{
    [Test]
    public void Construct_NullName()
    {
        Should.Throw<ArgumentNullException>(() =>
        {
            _ = new Seed(null!, "a");
        });
    }

    [Test]
    public void Construct_NullPath()
    {
        Should.Throw<ArgumentNullException>(() =>
        {
            _ = new Seed("a", null!);
        });
    }

    [Test]
    public void Name_Get()
    {
        new Seed("a", "b").Name.ShouldBe("a");
    }

    [Test]
    public void Path_Get()
    {
        new Seed("a", "b").Path.ShouldBe("b");
    }
}
