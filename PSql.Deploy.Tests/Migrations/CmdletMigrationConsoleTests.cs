// Copyright Subatomix Research Inc.
// SPDX-License-Identifier: MIT

namespace PSql.Deploy.Migrations;

using static M.MigrationPhase;

[TestFixture]
public class CmdletMigrationConsoleTests : TestHarnessBase
{
    private CmdletMigrationConsole? _console;
    private string?                 _logPath;

    private readonly Mock<ICmdlet>                 _cmdlet;
    private readonly Mock<M.IMigrationApplication> _info;
    private readonly Mock<M.IMigrationSession>     _session;
    private readonly E.Target                      _target;
    private readonly MockSequence                  _sequence;

    public CmdletMigrationConsoleTests()
    {
        _cmdlet   = Mocks.Create<ICmdlet>();
        _info     = Mocks.Create<M.IMigrationApplication>();
        _session  = Mocks.Create<M.IMigrationSession>();
        _target   = new E.Target("Server = .", null, "S", "D");
        _sequence = new();

        _info.Setup(i => i.Session).Returns(_session.Object);
        _info.Setup(i => i.Target ).Returns(_target);
    }

    private CmdletMigrationConsole Console
        => _console ??= new(_cmdlet.Object, _logPath);

    [Test]
    public void Constructor_NullCmdlet()
    {
        Should.Throw<ArgumentNullException>(() =>
        {
            _ = new CmdletMigrationConsole(null!, _logPath);
        });
    }

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

        WithCurrentPhase(Core);

        using (var log = Console.CreateLog(_info.Object))
        {
            log.ShouldNotBeNull();
            log.Write(random);
            log.Flush();
        }

        var text = File.ReadAllText(Path.Combine(_logPath, "S.D.1_Core.log"));

        text.ShouldBe(random);
    }

    [Test]
    public void ReportStarting()
    {
        WithCurrentPhase(Pre);

        ExpectWriteHost("[Pre] [D] ", newLine: false, ConsoleColor.Blue);
        ExpectWriteHost("Starting");

        Console.ReportStarting(_info.Object);
    }

    [Test]
    public void ReportApplying()
    {
        WithCurrentPhase(Core);

        ExpectWriteHost("[Core] [D] ", newLine: false, ConsoleColor.Blue);
        ExpectWriteHost("Applying A (Post)");

        Console.ReportApplying(_info.Object, "A", Post); // Post content being applied in Core phase
    }

    [Test]
    public void ReportApplied()
    {
        WithCurrentPhase(Post);

        ExpectWriteHost("[Post] [D] ", newLine: false, ConsoleColor.Blue);
        ExpectWriteHost("Applied 3 migration(s) in 12.345 second(s) [INCOMPLETE]");

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
        WithCurrentPhase(Pre);

        ExpectWriteHost("[Pre] [D] ", newLine: false, ConsoleColor.Yellow);
        ExpectWriteHost("Oops!",       newLine: true,  ConsoleColor.Yellow);

        Console.ReportProblem(_info.Object, "Oops!");
    }

    private void WithCurrentPhase(M.MigrationPhase phase)
    {
        _session.Setup(s => s.CurrentPhase).Returns(phase);
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
