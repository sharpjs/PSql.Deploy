// Copyright Subatomix Research Inc.
// SPDX-License-Identifier: MIT

namespace PSql.Deploy.Seeds;

using static SeedSessionOptions;

[TestFixture]
public class SeedSessionTests : TestHarnessBase
{
    // To make truth tables easier to read
    const bool Yes = true, ___ = false;

    private SeedSession?       _session;
    private SeedSessionOptions _options;

    private readonly Mock<ISeedConsole>                _console;
    //private readonly Mock<SeedTargetConnectionFactory> _factory;

    private SeedSession Session
        => _session ??= new SeedSession(_options, _console.Object);

    private ISeedSessionInternal SessionInternal
        => Session;

    public SeedSessionTests()
    {
        _console = Mocks.Create<ISeedConsole>();
        //_factory = Mocks.Create<SeedTargetConnectionFactory>();
    }

    protected override void CleanUp(bool managed)
    {
        _session?.Dispose();
        base.CleanUp(managed);
    }

    [Test]
    public void Construct_NullConsole()
    {
        Should.Throw<ArgumentNullException>(() =>
        {
            _ = new SeedSession(default, null!);
        });
    }

    [Test]
    public void Options_Get()
    {
        _options = IsWhatIfMode;

        Session.Options.ShouldBe(IsWhatIfMode);
    }

    [Test]
    public void IsWhatIfMode_Get_False()
    {
        Session.IsWhatIfMode.ShouldBeFalse();
    }

    [Test]
    public void IsWhatIfMode_Get_True()
    {
        _options = IsWhatIfMode;

        Session.IsWhatIfMode.ShouldBeTrue();
    }

    [Test]
    public void Seeds_Get_Initial()
    {
        Session.Seeds.ShouldBeEmpty();
    }

    [Test]
    public void DiscoverSeeds_NullPath()
    {
        Should.Throw<ArgumentNullException>(() =>
        {
            SessionInternal.DiscoverSeeds(null!, ["Seed0"]);
        });
    }

    [Test]
    public void Discover_NullNames()
    {
        Should.Throw<ArgumentNullException>(() =>
        {
            _ = SeedDiscoverer.Get("path", null!);
        });
    }

    [Test]
    public void Discover_NullName()
    {
        Should.Throw<ArgumentException>(() =>
        {
            _ = SeedDiscoverer.Get("path", [null!]);
        });
    }

    [Test]
    public void Discover_NotFound()
    {
        Should.Throw<FileNotFoundException>(() =>
        {
            _ = SeedDiscoverer.Get("nonexistent-path", ["nonexistent-seed"]);
        });
    }

    [Test]
    public void Discover_Ok()
    {
        var path = Path.Combine(
            TestContext.CurrentContext.TestDirectory, "TestDbs", "A"
        );

        var seeds = SeedDiscoverer.Get(path, ["Seed0"]);

        seeds.ShouldHaveSingleItem().AssignTo(out var seed);

        seed.Name.ShouldBe("Seed0");
        seed.Path.ShouldBe(Path.Combine(path, "Seeds", "Seed0", "_Main.sql"));
    }
}
