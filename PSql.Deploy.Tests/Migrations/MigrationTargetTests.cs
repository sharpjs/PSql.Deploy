// Copyright 2023 Subatomix Research Inc.
// SPDX-License-Identifier: ISC

using Moq.Protected;

namespace PSql.Deploy.Migrations;

[TestFixture]
public class MigrationTargetTests : TestHarnessBase
{
    private readonly MigrationTarget           _target;
    private readonly Mock<IMigrationSession>   _session;
    private readonly Mock<IMigrationInternals> _internals;
    private readonly SqlContext                _context;
    private readonly StringWriter              _log;

    public MigrationTargetTests()
    {
        _context = new()
        {
            ServerName   = "db.example.com",
            DatabaseName = "test",
        };

        _log = new StringWriter();

        _session = Mocks.Create<IMigrationSession>();
        _session
            .Setup(s => s.Phase)
            .Returns(MigrationPhase.Pre);
        _session
            .Setup(s => s.CreateLog("db.example.com.test.Pre.log"))
            .Returns(_log);

        _internals = Mocks.Create<IMigrationInternals>();

        _target = new MigrationTarget(_session.Object, _context)
        {
            Internals = _internals.Object
        };
    }

    protected override void CleanUp(bool managed)
    {
        _target.Dispose();

        base.CleanUp(managed);
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

        _log.ToString().Should().Contain("Nothing to do.");
    }

    [Test]
    public async Task ApplyAsync_Invalid()
    {
        var a = new Migration("a")
        {
            Path      = "/test/a",
            DependsOn = ImmutableArray.Create(new MigrationReference("a"))
        };

        _session
            .Setup(s => s.ReportStarting("test"))
            .Verifiable();

        _session
            .Setup(s => s.Migrations)
            .Returns(ImmutableArray.Create(a))
            .Verifiable();

        _session
            .Setup(s => s.GetAppliedMigrationsAsync(_target.Context, _target.LogConsole))
            .ReturnsAsync(new Migration[0])
            .Verifiable();

        _internals
            .Setup(i => i.LoadContent(a))
            .Callback(() => { a.IsContentLoaded = true; })
            .Verifiable();

        _session
            .Setup(s => s.ReportProblem(
                "Migration 'a' declares a dependency on itself. " +
                "The dependency cannot be satisfied."
            ))
            .Verifiable();

        _session
            .Setup(s => s.ReportApplied(
                "test", 0, It.Is<TimeSpan>(t => t >= TimeSpan.Zero),
                MigrationTargetDisposition.Successful // TODO: really?
            ))
            .Verifiable();

        await _target.ApplyAsync();

        _log.ToString().Should().ContainAll(
            "PSql.Deploy Migration Log",
            "Migration Phase:    Pre",
            "Pending Migrations: 1",
            "Error: Migration 'a' declares a dependency on itself. The dependency cannot be satisfied.",
            "Applied 0 migration(s)"
        );
    }

    [Test]
    public async Task ApplyAsync_EmptyPlan()
    {
        var aDefined = new Migration("a")
        {
            Path = "/test/a",
            Hash = "abc123",
        };

        var aApplied = new Migration("a")
        {
            State = MigrationState.AppliedCore,
            Hash  = "abc123",
        };

        _session
            .Setup(s => s.ReportStarting("test"))
            .Verifiable();

        _session
            .Setup(s => s.Migrations)
            .Returns(ImmutableArray.Create(aDefined))
            .Verifiable();

        _session
            .Setup(s => s.GetAppliedMigrationsAsync(_target.Context, _target.LogConsole))
            .ReturnsAsync(new[] { aApplied })
            .Verifiable();

        _internals
            .Setup(i => i.LoadContent(aDefined))
            .Callback(() => { aDefined.IsContentLoaded = true; })
            .Verifiable();

        _session
            .Setup(s => s.ReportApplied(
                "test", 0, It.Is<TimeSpan>(t => t >= TimeSpan.Zero),
                MigrationTargetDisposition.Successful
            ))
            .Verifiable();

        await _target.ApplyAsync();

        _log.ToString().Should().ContainAll(
            "PSql.Deploy Migration Log",
            "Migration Phase:    Pre",
            "Pending Migrations: 1",
            "Nothing to do for the current phase.",
            "Applied 0 migration(s)"
        );
    }

    [Test]
    public async Task ApplyAsync_CoreDisallowed()
    {
        var aDefined = new Migration("a")
        {
            Path = "/test/a",
            Hash = "abc123",
            Core = { IsRequired = true, Sql = "core-sql" },
        };

        var aApplied = new Migration("a")
        {
            State = MigrationState.NotApplied,
            Hash  = "abc123",
        };

        _session
            .Setup(s => s.ReportStarting("test"))
            .Verifiable();

        _session
            .Setup(s => s.Migrations)
            .Returns(ImmutableArray.Create(aDefined))
            .Verifiable();

        _session
            .Setup(s => s.GetAppliedMigrationsAsync(_target.Context, _target.LogConsole))
            .ReturnsAsync(new[] { aApplied })
            .Verifiable();

        _internals
            .Setup(i => i.LoadContent(aDefined))
            .Callback(() => { aDefined.IsContentLoaded = true; })
            .Verifiable();

        _session
            .Setup(s => s.ReportProblem(
                "One or more migration(s) to be applied to database [db.example.com].[test] "   +
                "requires the Core (downtime) phase, but the -AllowCorePhase switch was not "   +
                "present for the Invoke-SqlMigrations command.  To allow the Core phase, pass " +
                "the switch to the command.  Otherwise, ensure that all migrations begin with " +
                "a '--# PRE' or '--# POST' directive and that any '--# REQUIRES:' directives "  +
                "reference only migrations that have been completely applied."
            ))
            .Verifiable();

        _session
            .Setup(s => s.ReportApplied(
                "test", 0, It.Is<TimeSpan>(t => t >= TimeSpan.Zero),
                MigrationTargetDisposition.Successful // TODO: Really?
            ))
            .Verifiable();

        await _target.ApplyAsync();

        _log.ToString().Should().ContainAll(
            "PSql.Deploy Migration Log",
            "Migration Phase:    Pre",
            "Pending Migrations: 1",
            "All pending migrations are valid for the current phase.",
            "Error: One or more migration(s) to be applied to database [db.example.com].[test] " +
              "requires the Core (downtime) phase",
            "Applied 0 migration(s)"
        );
    }

    [Test]
    public async Task ApplyAsync_CoreAllowed()
    {
        using var cancellation = new CancellationTokenSource();

        var a = new Migration("a")
        {
            Path = "/test/a",
            Pre  = { IsRequired = true, Sql = "pre-sql"  },
            Core = { IsRequired = true, Sql = "core-sql" },
            IsContentLoaded = true,
        };

        _target.AllowCorePhase = true;

        _session
            .Setup(s => s.CancellationToken)
            .Returns(cancellation.Token);

        _session
            .Setup(s => s.HasErrors)
            .Returns(false);

        _session
            .Setup(s => s.ReportStarting("test"))
            .Verifiable();

        _session
            .Setup(s => s.Migrations)
            .Returns(ImmutableArray.Create(a))
            .Verifiable();

        _session
            .Setup(s => s.GetAppliedMigrationsAsync(_target.Context, _target.LogConsole))
            .ReturnsAsync(new Migration[0])
            .Verifiable();

        _internals
            .Setup(i => i.LoadContent(a))
            .Verifiable();

        var connection = Mocks.Create<ISqlConnection>();
        var command    = Mocks.Create<ISqlCommand>();
        var command2   = Mocks.Create<DbCommand>();

        _internals
            .Setup(i => i.Connect(_context, _target.LogConsole))
            .Returns(connection.Object)
            .Verifiable();

        connection
            .Setup(c => c.CreateCommand())
            .Returns(command.Object)
            .Verifiable();

        command
            .SetupSet(c => c.CommandTimeout = 0)
            .Verifiable();

        _session
            .Setup(s => s.ReportApplying("test", "a", MigrationPhase.Pre))
            .Verifiable();

        connection
            .Setup(c => c.ClearErrors())
            .Verifiable();

        command
            .SetupSet(c => c.CommandText = It.IsRegex("pre-sql"))
            .Verifiable();

        command
            .Setup(c => c.UnderlyingCommand)
            .Returns(command2.Object);

        command2
            .Setup(c => c.ExecuteNonQueryAsync(_target.CancellationToken))
            .ReturnsAsync(0)
            .Verifiable();

        connection
            .Setup(c => c.ThrowIfHasErrors())
            .Verifiable();

        command2
            .Protected().Setup("Dispose", ItExpr.IsAny<bool>());
            // Prevent spurious exception if GC collects command2

        command
            .Setup(c => c.Dispose())
            .Verifiable();

        connection
            .Setup(c => c.Dispose())
            .Verifiable();

        _session
            .Setup(s => s.ReportApplied(
                "test", 1, It.Is<TimeSpan>(t => t >= TimeSpan.Zero),
                MigrationTargetDisposition.Successful
            ))
            .Verifiable();

        await _target.ApplyAsync();

        _log.ToString().Should().ContainAll(
            "PSql.Deploy Migration Log",
            "Migration Phase:    Pre",
            "Pending Migrations: 1",
            "All pending migrations are valid for the current phase.",
            "Applied 1 migration(s)"
        );
    }

    [Test]
    public async Task ApplyAsync_EmptySql()
    {
        using var cancellation = new CancellationTokenSource();

        var a = new Migration("a")
        {
            Path = "/test/a",
            Pre  = { IsRequired = true, Sql = "" },
        };

        _session
            .Setup(s => s.CancellationToken)
            .Returns(cancellation.Token);

        _session
            .Setup(s => s.HasErrors)
            .Returns(false);

        _session
            .Setup(s => s.ReportStarting("test"))
            .Verifiable();

        _session
            .Setup(s => s.Migrations)
            .Returns(ImmutableArray.Create(a))
            .Verifiable();

        _session
            .Setup(s => s.GetAppliedMigrationsAsync(_target.Context, _target.LogConsole))
            .ReturnsAsync(new Migration[0])
            .Verifiable();

        _internals
            .Setup(i => i.LoadContent(a))
            .Verifiable();

        var connection = Mocks.Create<ISqlConnection>();
        var command    = Mocks.Create<ISqlCommand>();

        _internals
            .Setup(i => i.Connect(_context, _target.LogConsole))
            .Returns(connection.Object)
            .Verifiable();

        connection
            .Setup(c => c.CreateCommand())
            .Returns(command.Object)
            .Verifiable();

        command
            .SetupSet(c => c.CommandTimeout = 0)
            .Verifiable();

        _session
            .Setup(s => s.ReportApplying("test", "a", MigrationPhase.Pre))
            .Verifiable();

        connection
            .Setup(c => c.ClearErrors())
            .Verifiable();

        connection
            .Setup(c => c.ThrowIfHasErrors())
            .Verifiable();

        command
            .Setup(c => c.Dispose())
            .Verifiable();

        connection
            .Setup(c => c.Dispose())
            .Verifiable();

        _session
            .Setup(s => s.ReportApplied(
                "test", 1, It.Is<TimeSpan>(t => t >= TimeSpan.Zero),
                MigrationTargetDisposition.Successful
            ))
            .Verifiable();

        await _target.ApplyAsync();

        _log.ToString().Should().ContainAll(
            "PSql.Deploy Migration Log",
            "Migration Phase:    Pre",
            "Pending Migrations: 1",
            "All pending migrations are valid for the current phase.",
            "Applied 1 migration(s)"
        );
    }

    [Test]
    public async Task ApplyAsync_WhatIf()
    {
        using var cancellation = new CancellationTokenSource();

        var a = new Migration("a")
        {
            Path = "/test/a",
            Pre  = { IsRequired = true, Sql = "pre-sql" },
            IsContentLoaded = true,
        };

        _target.IsWhatIfMode = true;

        _session
            .Setup(s => s.CancellationToken)
            .Returns(cancellation.Token);

        _session
            .Setup(s => s.HasErrors)
            .Returns(false);

        _session
            .Setup(s => s.ReportStarting("test"))
            .Verifiable();

        _session
            .Setup(s => s.Migrations)
            .Returns(ImmutableArray.Create(a))
            .Verifiable();

        _session
            .Setup(s => s.GetAppliedMigrationsAsync(_target.Context, _target.LogConsole))
            .ReturnsAsync(new Migration[0])
            .Verifiable();

        _internals
            .Setup(i => i.LoadContent(a))
            .Verifiable();

        // TODO: Should this have happened?
        //_session
        //    .Setup(s => s.ReportApplying("test", "a", MigrationPhase.Pre))
        //    .Verifiable();

        _session
            .Setup(s => s.ReportApplied(
                "test", 0, It.Is<TimeSpan>(t => t >= TimeSpan.Zero),
                MigrationTargetDisposition.Successful
            ))
            .Verifiable();

        await _target.ApplyAsync();

        _log.ToString().Should().ContainAll(
            "PSql.Deploy Migration Log",
            "Migration Phase:    Pre",
            "Pending Migrations: 1",
            "All pending migrations are valid for the current phase.",
            "Applied 0 migration(s)"
            //       ^ TODO: Instead say what would have been done
        );
    }

    [Test]
    public async Task ApplyAsync_Exception()
    {
        using var cancellation = new CancellationTokenSource();

        var a = new Migration("a")
        {
            Path = "/test/a",
            Pre  = { IsRequired = true, Sql = "pre-sql" },
            IsContentLoaded = true,
        };

        _session
            .Setup(s => s.CancellationToken)
            .Returns(cancellation.Token);

        _session
            .Setup(s => s.HasErrors)
            .Returns(false);

        _session
            .Setup(s => s.ReportStarting("test"))
            .Verifiable();

        _session
            .Setup(s => s.Migrations)
            .Returns(ImmutableArray.Create(a))
            .Verifiable();

        _session
            .Setup(s => s.GetAppliedMigrationsAsync(_target.Context, _target.LogConsole))
            .ReturnsAsync(new Migration[0])
            .Verifiable();

        _internals
            .Setup(i => i.LoadContent(a))
            .Verifiable();

        _internals
            .Setup(i => i.Connect(_context, _target.LogConsole))
            .Throws(new Exception("Oops!"));

        _session
            .Setup(r => r.ReportProblem("Oops!"))
            .Verifiable();

        _session
            .Setup(s => s.ReportApplied(
                "test", 0, It.Is<TimeSpan>(t => t >= TimeSpan.Zero),
                MigrationTargetDisposition.Failed
            ))
            .Verifiable();

        await _target.Awaiting(t => t.ApplyAsync())
            .Should().ThrowAsync<Exception>().WithMessage("Oops!");

        _log.ToString().Should().ContainAll(
            "PSql.Deploy Migration Log",
            "Migration Phase:    Pre",
            "Pending Migrations: 1",
            "All pending migrations are valid for the current phase.",
            "Applied 0 migration(s)"
        );
    }

    [Test]
    public async Task ApplyAsync_Canceled()
    {
        using var cancellation = new CancellationTokenSource();

        var a = new Migration("a")
        {
            Path = "/test/a",
            Pre  = { IsRequired = true, Sql = "pre-sql" },
            IsContentLoaded = true,
        };

        _session
            .Setup(s => s.CancellationToken)
            .Returns(cancellation.Token);

        _session
            .Setup(s => s.HasErrors)
            .Returns(false);

        _session
            .Setup(s => s.ReportStarting("test"))
            .Verifiable();

        _session
            .Setup(s => s.Migrations)
            .Returns(ImmutableArray.Create(a))
            .Verifiable();

        _session
            .Setup(s => s.GetAppliedMigrationsAsync(_target.Context, _target.LogConsole))
            .ReturnsAsync(new Migration[0])
            .Verifiable();

        _internals
            .Setup(i => i.LoadContent(a))
            .Verifiable();

        _internals
            .Setup(i => i.Connect(_context, _target.LogConsole))
            .Throws(new OperationCanceledException());

        _session
            .Setup(s => s.ReportApplied(
                "test", 0, It.Is<TimeSpan>(t => t >= TimeSpan.Zero),
                MigrationTargetDisposition.Incomplete
            ))
            .Verifiable();

        await _target.Awaiting(t => t.ApplyAsync())
            .Should().ThrowAsync<OperationCanceledException>();

        _log.ToString().Should().ContainAll(
            "PSql.Deploy Migration Log",
            "Migration Phase:    Pre",
            "Pending Migrations: 1",
            "All pending migrations are valid for the current phase.",
            "Applied 0 migration(s)"
        );
    }

    [Test]
    public async Task ApplyAsync_ErrorInOtherThread()
    {
        using var cancellation = new CancellationTokenSource();

        var a = new Migration("a")
        {
            Path = "/test/a",
            Pre  = { IsRequired = true, Sql = "pre-sql" },
        };

        _session
            .Setup(s => s.CancellationToken)
            .Returns(cancellation.Token);

        _session
            .Setup(s => s.HasErrors)
            .Returns(true);

        _session
            .Setup(s => s.ReportStarting("test"))
            .Verifiable();

        _session
            .Setup(s => s.Migrations)
            .Returns(ImmutableArray.Create(a))
            .Verifiable();

        _session
            .Setup(s => s.GetAppliedMigrationsAsync(_target.Context, _target.LogConsole))
            .ReturnsAsync(new Migration[0])
            .Verifiable();

        _internals
            .Setup(i => i.LoadContent(a))
            .Verifiable();

        var connection = Mocks.Create<ISqlConnection>();
        var command    = Mocks.Create<ISqlCommand>();

        _internals
            .Setup(i => i.Connect(_context, _target.LogConsole))
            .Returns(connection.Object)
            .Verifiable();

        connection
            .Setup(c => c.CreateCommand())
            .Returns(command.Object)
            .Verifiable();

        command
            .SetupSet(c => c.CommandTimeout = 0)
            .Verifiable();

        command
            .Setup(c => c.Dispose())
            .Verifiable();

        connection
            .Setup(c => c.Dispose())
            .Verifiable();

        _session
            .Setup(s => s.ReportApplied(
                "test", 0, It.Is<TimeSpan>(t => t >= TimeSpan.Zero),
                MigrationTargetDisposition.Incomplete
            ))
            .Verifiable();

        await _target.ApplyAsync();

        _log.ToString().Should().ContainAll(
            "PSql.Deploy Migration Log",
            "Migration Phase:    Pre",
            "Pending Migrations: 1",
            "All pending migrations are valid for the current phase.",
            "Applied 0 migration(s)"
        );
    }
}
