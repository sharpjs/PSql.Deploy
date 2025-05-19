// Copyright Subatomix Research Inc.
// SPDX-License-Identifier: MIT

namespace PSql.Deploy.Migrations;

[TestFixture]
public class MigrationApplicatorTests : TestHarnessBase
{
    private readonly MigrationApplicator              _applicator;
    private readonly Mock<IMigrationSessionInternal>  _session;
    private readonly Mock<IMigrationConsole>          _console;
    private readonly Mock<IMigrationTargetConnection> _connection;
    private readonly MockSequence                     _sequence;
    private readonly Target                           _target;
    private readonly StringWriter                     _log;

    public MigrationApplicatorTests()
    {
        _target       = new("Server=db.example.com;Database=test;User ID=test;Password=test");
        _session      = Mocks.Create<IMigrationSessionInternal>();
        _console      = Mocks.Create<IMigrationConsole>();
        _connection   = Mocks.Create<IMigrationTargetConnection>();
        _sequence     = new();
        _log          = new();

        WithAllowContentInCorePhase(false);
        WithIsWhatIfMode(false);

        _session
            .Setup(s => s.CurrentPhase)
            .Returns(MigrationPhase.Pre);
        _session
            .Setup(s => s.Console)
            .Returns(_console.Object);
        _session
            .Setup(s => s.CancellationToken)
            .Returns(Cancellation.Token);

        _console
            .Setup(s => s.CreateLog(_session.Object, _target))
            .Returns(_log);

        _applicator = new MigrationApplicator(_session.Object, _target);
    }

    [Test]
    public void Construct_NullSession()
    {
        Should.Throw<ArgumentNullException>(() =>
        {
            _ = new MigrationApplicator(null!, _target);
        });
    }

    [Test]
    public void Construct_NullContext()
    {
        Should.Throw<ArgumentNullException>(() =>
        {
            _ = new MigrationApplicator(_session.Object, null!);
        });
    }

    [Test]
    public void Session_Get()
    {
        _applicator.Session.ShouldBeSameAs(_session.Object);
    }

    [Test]
    public void Console_Get()
    {
        _applicator.Console.ShouldBeSameAs(_console.Object);
    }

    [Test]
    public void Target_Get()
    {
        _applicator.Target.ShouldBeSameAs(_target);
    }

    [Test]
    public async Task ApplyAsync_NoPendingMigrations()
    {
        WithDefinedMigrations([]);

        ExpectReportStarting();
        ExpectConnect();
        ExpectGetAppliedMigrations(minimumName: "", []);
        ExpectReportApplied(0, TargetDisposition.Successful);

        await _applicator.ApplyAsync();

        LogShouldContainAll("Nothing to do.");
    }

    [Test]
    public async Task ApplyAsync_Invalid()
    {
        var definedA = new Migration("a")
        {
            Path      = "/test/a",
            DependsOn = ImmutableArray.Create(new MigrationReference("a"))
        };

        WithDefinedMigrations([definedA]);

        ExpectReportStarting();
        ExpectConnect();
        ExpectGetAppliedMigrations(minimumName: definedA.Name, []);
        ExpectLoadContent(definedA);
        ExpectReportProblem(
            "Migration 'a' declares a dependency on itself. " +
            "The dependency cannot be satisfied."
        );
        ExpectReportProblem("Migration validation failed.");
        ExpectReportApplied(0, TargetDisposition.Failed);

        await Should.ThrowAsync<MigrationException>(_applicator.ApplyAsync);

        LogShouldContainAll(
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
        var definedA = new Migration("a")
        {
            Path = "/test/a",
            Hash = "abc123",
        };

        var appliedA = new Migration("a")
        {
            State = MigrationState.AppliedCore,
            Hash  = "abc123",
        };

        WithDefinedMigrations([definedA]);

        ExpectReportStarting();
        ExpectConnect();
        ExpectGetAppliedMigrations(minimumName: definedA.Name, [appliedA]);
        ExpectLoadContent(definedA);
        ExpectReportApplied(0, TargetDisposition.Successful);

        await _applicator.ApplyAsync();

        LogShouldContainAll(
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
        var definedA = new Migration("a")
        {
            Path = "/test/a",
            Hash = "abc123",
            Core = { IsRequired = true, Sql = "core-sql" },
        };

        var appliedA = new Migration("a")
        {
            State = MigrationState.NotApplied,
            Hash  = "abc123",
        };

        WithDefinedMigrations([definedA]);

        ExpectReportStarting();
        ExpectConnect();
        ExpectGetAppliedMigrations(minimumName: definedA.Name, [appliedA]);
        ExpectLoadContent(definedA);
        ExpectReportProblem(
            "One or more migration(s) to be applied to database [db.example.com].[test] "   +
            "requires the Core (downtime) phase, but the -AllowCorePhase switch was not "   +
            "present for the Invoke-SqlMigrations command.  To allow the Core phase, pass " +
            "the switch to the command.  Otherwise, ensure that all migrations begin with " +
            "a '--# PRE' or '--# POST' directive and that any '--# REQUIRES:' directives "  +
            "reference only migrations that have been completely applied."
        );
        ExpectReportProblem("Migration validation failed.");
        ExpectReportApplied(0, TargetDisposition.Failed);

        await Should.ThrowAsync<MigrationException>(_applicator.ApplyAsync);

        LogShouldContainAll(
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
        var definedA = new Migration("a")
        {
            Path = "/test/a",
            Pre  = { IsRequired = true, Sql = "pre-sql"  },
            Core = { IsRequired = true, Sql = "core-sql" },
            IsContentLoaded = true,
        };

        WithDefinedMigrations([definedA]);
        WithAllowContentInCorePhase();

        ExpectReportStarting();
        ExpectConnect();
        ExpectGetAppliedMigrations(minimumName: definedA.Name, []);
        ExpectLoadContent(definedA);
        ExpectInitializeMigrationSupport();
        ExpectReportApplying("a", MigrationPhase.Pre);
        ExpectExecuteAsync("a", MigrationPhase.Pre);
        ExpectReportApplied(1, TargetDisposition.Successful);

        await _applicator.ApplyAsync();

        LogShouldContainAll(
            "PSql.Deploy Migration Log",
            "Migration Phase:    Pre",
            "Pending Migrations: 1",
            "All pending migrations are valid for the current phase.",
            "Applied 1 migration(s)"
        );
    }

    [Test]
    public async Task ApplyAsync_Exception()
    {
        ExpectReportStarting();
        ExpectConnectThrows("Oops!");
        ExpectReportProblem("Oops!");
        ExpectReportApplied(count: 0, TargetDisposition.Failed);

        var exception = await Should.ThrowAsync<Exception>(_applicator.ApplyAsync);
        exception.Message.ShouldBe("Oops!");

        LogShouldContainAll(
            "PSql.Deploy Migration Log",
            "Migration Phase:    Pre",
            "Applied 0 migration(s)"
        );
    }

    [Test]
    public async Task ApplyAsync_Canceled()
    {
        ExpectReportStarting();
        ExpectConnectCanceled();
        ExpectReportApplied(count: 0, TargetDisposition.Incomplete);

        await Should.ThrowAsync<OperationCanceledException>(_applicator.ApplyAsync);

        LogShouldContainAll(
            "PSql.Deploy Migration Log",
            "Migration Phase:    Pre",
            "Applied 0 migration(s)"
        );
    }

    private void WithDefinedMigrations(Migration[] migrations)
    {
        _session
            .Setup(s => s.Migrations)
            .Returns(ImmutableArray.Create(migrations))
            .Verifiable();
        _session
            .Setup(s => s.EarliestDefinedMigrationName)
            .Returns(migrations is [var m, ..] ? m.Name : "");
    }

    private void WithAllowContentInCorePhase(bool value = true)
    {
        _session
            .Setup(s => s.AllowContentInCorePhase)
            .Returns(value);
    }

    private void WithIsWhatIfMode(bool value = false)
    {
        _session
            .Setup(s => s.IsWhatIfMode)
            .Returns(value);
    }

    private void ExpectReportStarting()
    {
        _console
            .InSequence(_sequence)
            .Setup(c => c.ReportStarting(_session.Object, _target))
            .Verifiable();
    }

    private void ExpectConnect()
    {
        _session
            .InSequence(_sequence)
            .Setup(s => s.Connect(_target, It.IsNotNull<ISqlMessageLogger>()))
            .Returns(_connection.Object)
            .Verifiable();

        _connection
            .InSequence(_sequence)
            .Setup(c => c.OpenAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask)
            .Verifiable();

        // Require eventual disposal
        _connection
            .Setup(c => c.DisposeAsync())
            .Returns(ValueTask.CompletedTask)
            .Verifiable();
    }

    private void ExpectGetAppliedMigrations(string? minimumName, Migration[] migrations)
    {
        _connection
            .InSequence(_sequence)
            .Setup(c => c.GetAppliedMigrationsAsync(minimumName, Cancellation.Token))
            .ReturnsAsync(migrations)
            .Verifiable();
    }

    private void ExpectLoadContent(Migration migration)
    {
        _session
            .InSequence(_sequence)
            .Setup(i => i.LoadContent(migration))
            .Callback(() => { migration.IsContentLoaded = true; })
            .Verifiable();
    }

    private void ExpectConnectThrows(string error)
    {
        _session
            .InSequence(_sequence)
            .Setup(s => s.Connect(_target, It.IsNotNull<ISqlMessageLogger>()))
            .Throws(new Exception(error))
            .Verifiable();
    }

    private void ExpectConnectCanceled()
    {
        void Cancel(Target target, ISqlMessageLogger logger)
            => Cancellation.Cancel();

        _session
            .InSequence(_sequence)
            .Setup(s => s.Connect(_target, It.IsNotNull<ISqlMessageLogger>()))
            .Callback(Cancel)
            .Throws(new OperationCanceledException())
            .Verifiable();
    }

    private void ExpectInitializeMigrationSupport()
    {
        _connection
            .Setup(c => c.InitializeMigrationSupportAsync(Cancellation.Token))
            .Returns(Task.CompletedTask)
            .Verifiable();
    }

    private void ExpectReportApplying(string migrationName, MigrationPhase phase)
    {
        _console
            .InSequence(_sequence)
            .Setup(c => c.ReportApplying(_session.Object, _target, migrationName, phase))
            .Verifiable();
    }

    private void ExpectExecuteAsync(string migrationName, MigrationPhase phase)
    {
        _connection
            .InSequence(_sequence)
            .Setup(c => c.ExecuteMigrationContentAsync(
                It.Is<Migration>(m => m.Name == migrationName), phase, Cancellation.Token
            ))
            .Returns(Task.CompletedTask)
            .Verifiable();
    }

    private void ExpectReportProblem(string problem)
    {
        _console
            .InSequence(_sequence)
            .Setup(c => c.ReportProblem(_session.Object, _target, problem))
            .Verifiable();
    }

    private void ExpectReportApplied(int count, TargetDisposition disposition)
    {
        _console
            .InSequence(_sequence)
            .Setup(c => c.ReportApplied(
                _session.Object, _target,
                count, It.Is<TimeSpan>(t => t >= TimeSpan.Zero), disposition
            ))
            .Verifiable();
    }

    private void LogShouldContainAll(params string[] items)
    {
        var log = _log.ToString();

        foreach (var item in items)
            log.ShouldContain(item);
    }
}
