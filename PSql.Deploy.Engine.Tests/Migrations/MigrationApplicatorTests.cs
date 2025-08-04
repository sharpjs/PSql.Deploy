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

        _applicator = new MigrationApplicator(_session.Object, _target);

        _console
            .Setup(c => c.CreateLog(_applicator))
            .Returns(_log);
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
    public void IMigrationApplication_Session_Get()
    {
        ((IMigrationApplication) _applicator).Session.ShouldBeSameAs(_session.Object);
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
    public async Task ApplyAsync_NoMigrations()
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
            DependsOn = Refs("a"),
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

        //await _applicator.ApplyAsync(); // Uncomment to see the exception thrown
        await Should.ThrowAsync<MigrationException>(_applicator.ApplyAsync);

        LogShouldContainAll(
            "PSql.Deploy Migration Log",
            "Migration Phase:    Pre",
            "Pending Migrations: 1",
            "Error: Migration 'a' declares a dependency on itself.",
            "Applied 0 migration(s)"
        );
    }

    [Test]
    public async Task ApplyAsync_Missing()
    {
        var appliedA = new Migration("a")
        {
            State = MigrationState.AppliedPre,
            Hash  = "foo",
        };

        WithDefinedMigrations([]);
        WithCurrentPhase(MigrationPhase.Post);

        ExpectReportStarting();
        ExpectConnect();
        ExpectGetAppliedMigrations(minimumName: "", [appliedA]);
        //ExpectLoadContent(definedA); // because fully applied
        ExpectReportProblem(
            "Migration 'a' is only partially applied to database " +
            "[db.example.com].[test] (through the Pre phase), but the code " +
            "for the migration was not found in the source directory. It is " +
            "not possible to complete this migration."
        );
        ExpectReportProblem("Migration validation failed.");
        ExpectReportApplied(0, TargetDisposition.Failed);

        //await _applicator.ApplyAsync(); // Uncomment to see the exception thrown
        await Should.ThrowAsync<MigrationException>(_applicator.ApplyAsync);

        LogShouldContainAll(
            "PSql.Deploy Migration Log",
            "Migration Phase:    Post",
            "Pending Migrations: 1",
            " Missing ",
            "Error: Migration 'a' is only partially applied ",
            "Applied 0 migration(s)"
        );
    }

    [Test]
    public async Task ApplyAsync_Changed()
    {
        var definedA = new Migration("a")
        {
            Path = "/test/a",
            Hash = "600D",
        };

        var appliedA = new Migration("a")
        {
            State = MigrationState.AppliedPost,
            Hash  = "D1FF", // different hash
        };

        WithDefinedMigrations([definedA]);

        ExpectReportStarting();
        ExpectConnect();
        ExpectGetAppliedMigrations(minimumName: definedA.Name, [appliedA]);
        //ExpectLoadContent(definedA); // because fully applied
        ExpectReportProblem(
            "Migration 'a' has been applied to database [db.example.com].[test] " +
            "through the Post phase, but the migration's code in the source " +
            "directory does not match the code previously used. To resolve, " +
            "revert any accidental changes to this migration. To ignore, " +
            "update the hash in the _deploy.Migration table to match the hash " +
            "of the code in the source directory (600D).");
        ExpectReportProblem("Migration validation failed.");
        ExpectReportApplied(0, TargetDisposition.Failed);

        //await _applicator.ApplyAsync(); // Uncomment to see the exception thrown
        await Should.ThrowAsync<MigrationException>(_applicator.ApplyAsync);

        LogShouldContainAll(
            "PSql.Deploy Migration Log",
            "Migration Phase:    Pre",
            "Pending Migrations: 1",
            "Error: Migration 'a' has been applied to database ",
            " code in the source directory does not match ",
            "Applied 0 migration(s)"
        );
    }

    [Test]
    public async Task ApplyAsync_ValidWithWarnings()
    {
        var definedB = new Migration("b")
        {
            Path      = "/test/b",
            DependsOn = Refs("a"),
        };

        WithDefinedMigrations([definedB]);

        ExpectReportStarting();
        ExpectConnect();
        ExpectGetAppliedMigrations(minimumName: definedB.Name, []);
        ExpectLoadContent(definedB);
        ExpectReportProblem(
            "Ignoring migration 'b' dependency on migration 'a', " +
            "which is older than the earliest migration on disk."
        );
        ExpectInitializeMigrationSupport();
        ExpectReportApplying("b", MigrationPhase.Pre);
        ExpectExecuteAsync("b", MigrationPhase.Pre);
        ExpectReportApplied(1, TargetDisposition.Successful);

        await _applicator.ApplyAsync();

        LogShouldContainAll(
            "PSql.Deploy Migration Log",
            "Migration Phase:    Pre",
            "Pending Migrations: 1",
            "Warning: Ignoring migration 'b' dependency on migration 'a',",
            "Applied 1 migration(s)"
        );
    }

    [Test]
    public async Task ApplyAsync_PhaseAlreadyDone()
    {
        const string
            BeginName = Migration.BeginPseudoMigrationName,
            EndName   = Migration.EndPseudoMigrationName;

        var begin    = new Migration(BeginName) { Path = "/test/" + BeginName };
        var end      = new Migration(EndName)   { Path = "/test/" + EndName   };

        var definedA = new Migration("a") { Path = "/test/a", Hash = "foo" };
        var definedB = new Migration("b") { Path = "/test/b", Hash = "bar" };
        var definedC = new Migration("c") { Path = "/test/c", Hash = "baz" };

        var appliedA = new Migration("a") { State = MigrationState.AppliedPre,  Hash = "foo" };
        var appliedB = new Migration("b") { State = MigrationState.AppliedCore, Hash = "bar" };
        var appliedC = new Migration("c") { State = MigrationState.AppliedPost, Hash = "baz" };

        WithDefinedMigrations([begin, definedA, definedB, definedC, end]);

        ExpectReportStarting();
        ExpectConnect();
        ExpectGetAppliedMigrations(minimumName: definedA.Name, [appliedA, appliedB, appliedC]);
        ExpectLoadContent(begin);
        ExpectLoadContent(definedA);
        ExpectLoadContent(definedB);
        //pectLoadContent(definedC); // skipped because fully applied
        ExpectLoadContent(end);
        ExpectReportApplied(0, TargetDisposition.Successful);

        await _applicator.ApplyAsync();

        LogShouldContainAll(
            "PSql.Deploy Migration Log",
            "Migration Phase:    Pre",
            "Pending Migrations: 5", // TODO: I'm not sure this count is truly the 'pending' count. Maybe defined, applied, and pending counts separately?
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

        //await _applicator.ApplyAsync(); // Uncomment to see the exception thrown
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
    public async Task ApplyAsync_Dependency()
    {
        var definedA = new Migration("a")
        {
            Path            = "/test/a",
            Post            = { IsRequired = true, Sql = "post-sql" },
            IsContentLoaded = true,
        };

        var definedB = new Migration("b")
        {
            Path            = "/test/a",
            Pre             = { IsRequired = true, Sql = "pre-sql" },
            DependsOn       = Refs("a"),
            IsContentLoaded = true,
        };

        WithDefinedMigrations([definedA, definedB]);
        WithAllowContentInCorePhase();

        ExpectReportStarting();
        ExpectConnect();
        ExpectGetAppliedMigrations(minimumName: definedA.Name, []);
        ExpectLoadContent(definedA);
        ExpectLoadContent(definedB);
        ExpectInitializeMigrationSupport();
        ExpectReportApplying("a", MigrationPhase.Pre);
        ExpectExecuteAsync("a", MigrationPhase.Pre);
        ExpectReportApplied(1, TargetDisposition.Successful);

        await _applicator.ApplyAsync();

        LogShouldContainAll(
            "PSql.Deploy Migration Log",
            "Migration Phase:    Pre",
            "Pending Migrations: 2",
            "All pending migrations are valid for the current phase.",
            "Applied 1 migration(s)"
        );
    }

    [Test]
    public async Task ApplyAsync_Exception()
    {
        var exception = new DataException("Database is on fire.");

        ExpectReportStarting();
        ExpectConnectThrows(exception);
        ExpectReportProblem("Database is on fire.");
        ExpectReportApplied(count: 0, TargetDisposition.Failed);

        await Should.ThrowAsync<DataException>(_applicator.ApplyAsync);

        LogShouldContainAll(
            "PSql.Deploy Migration Log",
            "Migration Phase:    Pre",
            "System.Data.DataException: Database is on fire.",
            "Applied 0 migration(s)"
        );
    }

    [Test]
    public async Task ApplyAsync_Canceled()
    {
        ExpectReportStarting();
        ExpectConnectCanceled();
        ExpectReportApplied(count: 0, TargetDisposition.Incomplete);

        //await _applicator.ApplyAsync(); // Uncomment to see the exception thrown
        await Should.ThrowAsync<OperationCanceledException>(_applicator.ApplyAsync);

        LogShouldContainAll(
            "PSql.Deploy Migration Log",
            "Migration Phase:    Pre",
            "Migration application was canceled.",
            "Applied 0 migration(s)"
        );
    }

    private static ImmutableArray<MigrationReference> Refs(string name)
    {
        return ImmutableArray.Create(new MigrationReference(name));
    }

    private void WithCurrentPhase(MigrationPhase phase)
    {
        _session
            .Setup(s => s.CurrentPhase)
            .Returns(phase);
    }

    private void WithDefinedMigrations(Migration[] migrations)
    {
        var earliestName = migrations.FirstOrDefault(m => !m.IsPseudo)?.Name ?? "";

        _session
            .Setup(s => s.Migrations)
            .Returns(ImmutableArray.Create(migrations))
            .Verifiable();
        _session
            .Setup(s => s.EarliestDefinedMigrationName)
            .Returns(earliestName);
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
            .Setup(c => c.ReportStarting(_applicator))
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

    private void ExpectConnectThrows(Exception exception)
    {
        _session
            .InSequence(_sequence)
            .Setup(s => s.Connect(_target, It.IsNotNull<ISqlMessageLogger>()))
            .Throws(exception)
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
            .Setup(c => c.ReportApplying(_applicator, migrationName, phase))
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
            .Setup(c => c.ReportProblem(_applicator, problem))
            .Verifiable();
    }

    private void ExpectReportApplied(int count, TargetDisposition disposition)
    {
        _console
            .InSequence(_sequence)
            .Setup(c => c.ReportApplied(
                _applicator, count, It.Is<TimeSpan>(t => t >= TimeSpan.Zero), disposition
            ))
            .Verifiable();
    }

    private void LogShouldContainAll(params string[] items)
    {
        var log = _log.ToString();

        try
        {
            foreach (var item in items)
                log.ShouldContain(item);
        }
        //finally   // To see the log for all tests
        catch       // To see the log only when it's wrong
        {
            TestContext.Write(log);
        }
    }
}
