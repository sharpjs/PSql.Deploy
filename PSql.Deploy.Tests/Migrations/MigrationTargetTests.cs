// Copyright 2023 Subatomix Research Inc.
// SPDX-License-Identifier: ISC

namespace PSql.Deploy.Migrations;

[TestFixture]
public class MigrationTargetTests : TestHarnessBase
{
    private readonly MigrationTarget         _target;
    private readonly Mock<IMigrationSession> _session;
    private readonly SqlContext              _context;
    private readonly StringWriter            _log;

    public MigrationTargetTests()
    {
        _session = Mocks.Create<IMigrationSession>();
        _context = new()
        {
            ServerName   = "db.example.com",
            DatabaseName = "test",
        };

        _session
            .Setup(s => s.Phase)
            .Returns(MigrationPhase.Pre);

        _log = new StringWriter();
        _session
            .Setup(s => s.CreateLog("db.example.com.test.Pre.log"))
            .Returns(_log);

        _target = new MigrationTarget(_session.Object, _context);
    }

    [Test]
    public void Construct_NullSession()
    {
        Invoking(() => new MigrationTarget(null!, _context))
            .Should().Throw<ArgumentNullException>();
    }

    [Test]
    public void Construct_NullContext()
    {
        Invoking(() => new MigrationTarget(_session.Object, null!))
            .Should().Throw<ArgumentNullException>();
    }

    [Test]
    public void Session_Get()
    {
        _target.Session.Should().BeSameAs(_session.Object);
    }

    [Test]
    public void Context_Get()
    {
        _target.Context.Should().BeSameAs(_context);
    }

    [Test]
    public void EarliestDefinedMigrationName_Get()
    {
        var value = "a";

        _session.Setup(s => s.MinimumMigrationName).Returns(value);

        _target.EarliestDefinedMigrationName.Should().BeSameAs(value);
    }

    [Test]
    public void Phase_Get()
    {
        var value = MigrationPhase.Core;

        _session.Setup(s => s.Phase).Returns(value);

        _target.Phase.Should().Be(value);
    }

    [Test]
    public void CancellationToken_Get()
    {
        using var cancellation = new CancellationTokenSource();

        _session.Setup(s => s.CancellationToken).Returns(cancellation.Token);

        _target.CancellationToken.Should().Be(cancellation.Token);
    }

    [Test]
    public void ServerName_Get()
    {
        _target.ServerName.Should().Be(_context.ServerName);
    }

    [Test]
    public void DatabaseName_Get()
    {
        _target.DatabaseName.Should().Be(_context.DatabaseName);
    }

    [Test]
    public void LogWriter_Get()
    {
        _target.LogWriter.Should().BeSameAs(_log);
    }

    [Test]
    public void LogConsole_Get()
    {
        _target.LogConsole.Should().BeOfType<TextWriterConsole>();
    }

    [Test]
    public void AllowCorePhase_Get()
    {
        _target.AllowCorePhase.Should().BeFalse();
    }

    [Test]
    public void IsWhatIfMode_Get()
    {
        _target.IsWhatIfMode.Should().BeFalse();
    }

    [Test]
    public async Task ApplyAsync_NoPendingMigrations()
    {
        _session
            .Setup(s => s.ReportStarting("test"))
            .Verifiable();

        _session
            .Setup(s => s.Migrations)
            .Returns(ImmutableArray<Migration>.Empty)
            .Verifiable();

        _session
            .Setup(s => s.GetAppliedMigrationsAsync(_target.Context, _target.LogConsole))
            .ReturnsAsync(new Migration[0])
            .Verifiable();

        _session
            .Setup(s => s.ReportApplied(
                "test", 0, It.Is<TimeSpan>(t => t >= TimeSpan.Zero),
                MigrationTargetDisposition.Successful
            ))
            .Verifiable();

        await _target.ApplyAsync();
    }
}
