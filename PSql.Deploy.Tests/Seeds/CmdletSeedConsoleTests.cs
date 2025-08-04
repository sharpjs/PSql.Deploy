// Copyright Subatomix Research Inc.
// SPDX-License-Identifier: MIT

namespace PSql.Deploy.Seeds;

[TestFixture]
public class CmdletSeedConsoleTests : TestHarnessBase
{
    private CmdletSeedConsole? _console;
    private string?            _logPath;

    private readonly Mock<ICmdlet>            _cmdlet;
    private readonly Mock<S.ISeedApplication> _info;
    private readonly Mock<S.ISeedSession>     _session;
    private readonly E.Target                 _target;
    private readonly S.LoadedSeed             _seed;
    private readonly MockSequence             _sequence;

    public CmdletSeedConsoleTests()
    {
        _cmdlet   = Mocks.Create<ICmdlet>();
        _info     = Mocks.Create<S.ISeedApplication>();
        _session  = Mocks.Create<S.ISeedSession>();
        _target   = new("Server = .", null, "S", "D");
        _seed     = new(new("Seed1", "/test/Seed1/_Main.sql"), ImmutableArray<S.SeedModule>.Empty);
        _sequence = new();

        _info.Setup(i => i.Session).Returns(_session.Object);
        _info.Setup(i => i.Target ).Returns(_target);
        _info.Setup(i => i.Seed   ).Returns(_seed);
    }

    private CmdletSeedConsole Console
        => _console ??= new(_cmdlet.Object, _logPath);

    [Test]
    public void CreateLog_NullLogPath()
    {
        Console
            .CreateLog(_info.Object)
            .ShouldBeSameAs(TextWriter.Null);
    }

    [Test]
    public void CreateLog_NonNullLogPath()
    {
        var context = TestContext.CurrentContext;
        var random  = context.Random.GetString(16);

        _logPath = Path.Combine(context.TestDirectory, context.Test.Name);

        using (var log = Console.CreateLog(_info.Object))
        {
            log.ShouldNotBeNull();
            log.Write(random);
            log.Flush();
        }

        var text = File.ReadAllText(Path.Combine(_logPath, "S.D.Seed1.log"));

        text.ShouldBe(random);
    }

    [Test]
    public void ReportStarting()
    {
        ExpectWriteHost("[Seed1] [D] ", newLine: false, ConsoleColor.Blue);
        ExpectWriteHost("Starting");

        Console.ReportStarting(_info.Object);
    }

    [Test]
    public void ReportApplying()
    {
        ExpectWriteHost("[Seed1] [D] ", newLine: false, ConsoleColor.Blue);
        ExpectWriteHost("Applying A");

        Console.ReportApplying(_info.Object, "A");
    }

    [Test]
    public void ReportApplied()
    {
        ExpectWriteHost("[Seed1] [D] ", newLine: false, ConsoleColor.Blue);
        ExpectWriteHost("Applied 3 module(s) in 12.345 second(s) [INCOMPLETE]");

        Console.ReportApplied(
            _info.Object,
            count:       3,
            duration:    new(12345L * TimeSpan.TicksPerMillisecond),
            disposition: E.TargetDisposition.Incomplete
        );
    }

    [Test]
    public void ReportProblem()
    {
        ExpectWriteHost("[Seed1] [D] ", newLine: false, ConsoleColor.Yellow);
        ExpectWriteHost("Oops!",        newLine: true,  ConsoleColor.Yellow);

        Console.ReportProblem(_info.Object, "Oops!");
    }

    private void ExpectWriteHost(
        string?       message,
        bool          newLine         = true,
        ConsoleColor? foregroundColor = null,
        ConsoleColor? backgroundColor = null)
    {
        _cmdlet
            .InSequence(_sequence)
            .Setup(c => c.WriteHost(message, newLine, foregroundColor, backgroundColor))
            .Verifiable();
    }
}
