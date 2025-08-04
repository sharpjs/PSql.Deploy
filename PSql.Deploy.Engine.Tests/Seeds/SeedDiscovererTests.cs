// Copyright Subatomix Research Inc.
// SPDX-License-Identifier: MIT

namespace PSql.Deploy.Seeds;

[TestFixture]
public class SeedDiscovererTests
{
    [Test]
    public void Get_NullPath()
    {
        Should.Throw<ArgumentNullException>(() =>
        {
            _ = SeedDiscoverer.Get(null!, ["some-seed"]);
        });
    }

    [Test]
    public void Get_NullNames()
    {
        Should.Throw<ArgumentNullException>(() =>
        {
            _ = SeedDiscoverer.Get("path", null!);
        });
    }

    [Test]
    public void Get_NullName()
    {
        Should.Throw<ArgumentException>(() =>
        {
            _ = SeedDiscoverer.Get("path", [null!]);
        });
    }

    [Test]
    public void Get_NotFound()
    {
        Should.Throw<FileNotFoundException>(() =>
        {
            _ = SeedDiscoverer.Get("nonexistent-path", ["nonexistent-seed"]);
        });
    }

    [Test]
    public void Get_Ok()
    {
        var path = Path.Combine(TestDirectory, "TestDbs", "A");

        var seeds = SeedDiscoverer.Get(path, ["Typical"]);

        seeds.ShouldHaveSingleItem().AssignTo(out var seed);

        seed.Name.ShouldBe("Typical");
        seed.Path.ShouldBe(Path.Combine(path, "Seeds", "Typical", "_Main.sql"));
    }

    private string TestDirectory { get; }
        = TestContext.CurrentContext.TestDirectory;
}
