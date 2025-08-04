// Copyright Subatomix Research Inc.
// SPDX-License-Identifier: MIT

namespace PSql.Deploy.Migrations;

using static MigrationPhase;

[TestFixture]
public class MigrationSessionTests : TestHarnessBase
{
    private MigrationSession? _session;

    private readonly MigrationSessionOptions                _options;
    private readonly Mock<IMigrationConsole>                _console;
    private readonly Mock<MigrationTargetConnectionFactory> _factory;

    private static readonly Target
        TargetA = new("Server=sql.example.com;Database=a"),
        TargetB = new("Server=sql.example.com;Database=b"),
        TargetC = new("Server=sql.example.com;Database=c");

    private MigrationSession Session
        => _session ??= new MigrationSession(_options, _console.Object);

    private IMigrationSessionInternal SessionInternal
        => Session;

    public MigrationSessionTests()
    {
        _options = new();
        _console = Mocks.Create<IMigrationConsole>();
        _factory = Mocks.Create<MigrationTargetConnectionFactory>();
    }

    protected override void CleanUp(bool managed)
    {
        _session?.Dispose();

        base.CleanUp(managed);
    }

    [Test]
    public void Constructor_NullConsole()
    {
        Should.Throw<ArgumentNullException>(() =>
        {
            return new MigrationSession(_options, null!);
        });
    }

    [Test]
    public void Constructor_NoEnabledPhases()
    {
        _options.EnabledPhases = [];

        Should.Throw<ArgumentException>(() =>
        {
            return new MigrationSession(_options, _console.Object);
        });
    }

    [Test]
    public void Console_Get()
    {
        Session.Console.ShouldBeSameAs(_console.Object);
    }

    [Test]
    public void EnabledPhases_Get()
    {
        _options.EnabledPhases = [Pre, Post];

        Session.EnabledPhases.ShouldBe([Pre, Post]);
    }

    [Test]
    public void AllowContentInCorePhase_Get_False()
    {
        Session.AllowContentInCorePhase.ShouldBeFalse();
    }

    [Test]
    public void AllowContentInCorePhase_Get_True()
    {
        _options.AllowContentInCorePhase = true;

        Session.AllowContentInCorePhase.ShouldBeTrue();
    }

    [Test]
    public void IsWhatIfMode_Get_False()
    {
        Session.IsWhatIfMode.ShouldBeFalse();
    }

    [Test]
    public void IsWhatIfMode_Get_True()
    {
        _options.IsWhatIfMode = true;

        Session.IsWhatIfMode.ShouldBeTrue();
    }

    [Test]
    public void CurrentPhase_Get()
    {
        _options.EnabledPhases = [Core, Post];

        Session.CurrentPhase.ShouldBe(Core);
    }

    [Test]
    public void Migrations_Get_Initial()
    {
        Session.Migrations.ShouldBeEmpty();
    }

    [Test]
    public void EarliestDefinedMigrationName_Get_Initial()
    {
        Session.EarliestDefinedMigrationName.ShouldBeEmpty();
    }

    [Test]
    public void DiscoverMigrations_NullPath()
    {
        Should.Throw<ArgumentNullException>(() =>
        {
            Session.DiscoverMigrations(null!);
        });
    }

    [Test]
    public void DiscoverMigrations_Empty()
    {
        var path = Path.Combine(TestContext.CurrentContext.TestDirectory, "TestDbs", "Empty");

        Session.DiscoverMigrations(path);

        Session.Migrations.Length           .ShouldBe(0);
        Session.EarliestDefinedMigrationName.ShouldBeEmpty();
    }

    [Test]
    public void DiscoverMigrations_Ok()
    {
        var path = Path.Combine(TestContext.CurrentContext.TestDirectory, "TestDbs", "A");

        Session.DiscoverMigrations(path);

        Session.Migrations.Length .ShouldBe(5);
        Session.Migrations[0].Name.ShouldBe("_Begin");
        Session.Migrations[1].Name.ShouldBe("Migration0");
        Session.Migrations[2].Name.ShouldBe("Migration1");
        Session.Migrations[3].Name.ShouldBe("Migration2");
        Session.Migrations[4].Name.ShouldBe("_End");

        Session.EarliestDefinedMigrationName.ShouldBe("Migration0");
    }

    [Test]
    public void DiscoverMigrations_Ok_WithLatestName()
    {
        var path = Path.Combine(TestContext.CurrentContext.TestDirectory, "TestDbs", "A");

        Session.DiscoverMigrations(path, latestName: "Migration1");

        Session.Migrations.Length .ShouldBe(4);
        Session.Migrations[0].Name.ShouldBe("_Begin");
        Session.Migrations[1].Name.ShouldBe("Migration0");
        Session.Migrations[2].Name.ShouldBe("Migration1");
        Session.Migrations[3].Name.ShouldBe("_End");

        Session.EarliestDefinedMigrationName.ShouldBe("Migration0");
    }

    [Test]
    public void GetRegisteredMigrationsAsync_NullTarget()
    {
        Should.Throw<ArgumentNullException>(() =>
        {
            return Session.GetRegisteredMigrationsAsync(null!);
        });
    }

    [Test]
    public async Task GetRegisteredMigrationsAsync_NullLogger()
    {
        await TestGetRegisteredMigrationsAsync(null);
    }

    [Test]
    public async Task GetRegisteredMigrationsAsync_Ok()
    {
        var logger = Mocks.Create<ISqlMessageLogger>();

        await TestGetRegisteredMigrationsAsync(logger.Object);
    }

    private async Task TestGetRegisteredMigrationsAsync(ISqlMessageLogger? logger)
    {
        var target     = new Target("Server=.;Database=a");
        var migration0 = new Migration("Migration0");
        var migration1 = new Migration("Migration1");

        var t = ForTarget(target);

        t.ExpectCreateAndOpenConnection();
        t.ExpectGetRegisteredMigrations("Migration0", migration0, migration1);
        t.ExpectDisposeConnection();

        var migrations = await Session.GetRegisteredMigrationsAsync(target, "Migration0", logger);

        migrations.ShouldBe([migration0, migration1]);
    }

    [Test]
    public void Connect_NullTarget()
    {
        var logger = Mocks.Create<ISqlMessageLogger>();

        Should.Throw<ArgumentNullException>(() =>
        {
            SessionInternal.Connect(null!, logger.Object);
        });
    }

    [Test]
    public void Connect_NullLogger()
    {
        Should.Throw<ArgumentNullException>(() =>
        {
            SessionInternal.Connect(TargetA, null!);
        });
    }

    [Test]
    public void Connect_Normal()
    {
        var logger = Mocks.Create<ISqlMessageLogger>();

        using var connection = SessionInternal.Connect(TargetA, logger.Object);

        connection       .ShouldBeOfType<SqlMigrationTargetConnection>();
        connection.Target.ShouldBeSameAs(TargetA);
        connection.Logger.ShouldBeSameAs(logger.Object);
    }

    [Test]
    public void Connect_WhatIf()
    {
        _options.IsWhatIfMode = true;

        var logger = Mocks.Create<ISqlMessageLogger>();

        using var connection = SessionInternal.Connect(TargetA, logger.Object);

        connection       .ShouldBeOfType<WhatIfMigrationTargetConnection>();
        connection.Target.ShouldBeSameAs(TargetA);
        connection.Logger.ShouldBeSameAs(logger.Object);
    }

    [Test]
    public void LoadContent_NullMigration()
    {
        Should.Throw<ArgumentNullException>(() =>
        {
            SessionInternal.LoadContent(null!);
        });
    }

    [Test]
    public void LoadContent_NullMigrationPath()
    {
        var migration = new Migration("Test");

        Should.Throw<ArgumentException>(() =>
        {
            SessionInternal.LoadContent(migration);
        });
    }

    [Test]
    public void LoadContent_Ok()
    {
        var path = Path.Combine(TestContext.CurrentContext.TestDirectory, "TestDbs", "A");

        Session.DiscoverMigrations(path);
        Session.Migrations.Length.ShouldBe(5);

        var migration = Session.Migrations[1];
        migration.Name           .ShouldBe("Migration0");
        migration.IsContentLoaded.ShouldBeFalse();

        SessionInternal.LoadContent(migration);

        migration.IsContentLoaded.ShouldBeTrue();
        migration.Pre .IsRequired.ShouldBeTrue();
        migration.Pre .Sql       .ShouldNotBeNullOrEmpty();
        migration.Post.IsRequired.ShouldBeTrue();
        migration.Post.Sql       .ShouldNotBeNullOrEmpty();
    }

    [Test]
    public async Task Apply_Target_Exception()
    {
        _options.EnabledPhases = [Pre];
        var path = Path.Combine(TestContext.CurrentContext.TestDirectory, "TestDbs", "A");

        var innerException = new InvalidOperationException("Test exception.");

        var t = ForTarget(TargetA);
        t.ExpectCreateLog ();
        t.ExpectReportStarting();
        t.ExpectCreateAndOpenConnection(innerException);
        t.ExpectDisposeConnection();
        t.ExpectReportProblem("Test exception.");

        Session.DiscoverMigrations(path);
        Session.Migrations.Length.ShouldBe(5);

        Session.BeginApplying(TargetA);

        var outerException = await Should.ThrowAsync<MigrationException>(() =>
        {
            return Session.CompleteApplyingAsync(Cancellation.Token);
        });

        outerException.InnerException.ShouldBeSameAs(innerException);
    }

    [Test]
    public async Task Apply_Target_OnePhase()
    {
        _options.EnabledPhases = [Pre];

        var path = Path.Combine(TestContext.CurrentContext.TestDirectory, "TestDbs", "A");

        var t = ForTarget(TargetA);
        ExpectApplyMigrations(t, Pre);

        Session.DiscoverMigrations(path);
        Session.Migrations.Length.ShouldBe(5);

        Session.BeginApplying(TargetA);

        await Session.CompleteApplyingAsync(Cancellation.Token);
    }

    [Test]
    public async Task Apply_Target_MultiplePhases()
    {
        _options.EnabledPhases = [Pre, Post];

        var path = Path.Combine(TestContext.CurrentContext.TestDirectory, "TestDbs", "A");

        var t0 = ForTarget(TargetA);
        t0.ExpectCreateLog                 ();
        t0.ExpectReportStarting            ();
        t0.ExpectCreateAndOpenConnection   ();
        t0.ExpectGetRegisteredMigrations   ("Migration0");
        t0.ExpectInitializeMigrationSupport();
        t0.ExpectReportApplying            ("_Begin",     Pre);
        t0.ExpectApplyMigrationContent     ("_Begin",     Pre);
        t0.ExpectReportApplying            ("Migration0", Pre);
        t0.ExpectApplyMigrationContent     ("Migration0", Pre);
        t0.ExpectReportApplying            ("Migration1", Pre);
        t0.ExpectApplyMigrationContent     ("Migration1", Pre);
        t0.ExpectReportApplying            ("Migration2", Pre);
        t0.ExpectApplyMigrationContent     ("Migration2", Pre);
        t0.ExpectReportApplying            ("_End",       Pre);
        t0.ExpectApplyMigrationContent     ("_End",       Pre);
        t0.ExpectDisposeConnection         ();
        t0.ExpectReportApplied             (count: 5, TargetDisposition.Successful);

        var t1 = t0.Then(); // Reconnect for Post phase
        t1.ExpectCreateLog                 ();
        t1.ExpectReportStarting            ();
        t1.ExpectCreateAndOpenConnection   ();
        t1.ExpectGetRegisteredMigrations   ("Migration0", [
            new("Migration0") { State = MigrationState.AppliedPre },
            new("Migration1") { State = MigrationState.AppliedPre },
            new("Migration2") { State = MigrationState.AppliedPre }
        ]);
        //.ExpectInitializeMigrationSupport(); // Already initialized in Pre phase
        t1.ExpectReportApplying            ("_Begin",     Post);
        t1.ExpectApplyMigrationContent     ("_Begin",     Post);
        t1.ExpectReportApplying            ("Migration0", Post);
        t1.ExpectApplyMigrationContent     ("Migration0", Post);
        t1.ExpectReportApplying            ("Migration1", Post);
        t1.ExpectApplyMigrationContent     ("Migration1", Post);
        t1.ExpectReportApplying            ("Migration2", Post);
        t1.ExpectApplyMigrationContent     ("Migration2", Post);
        t1.ExpectReportApplying            ("_End",       Post);
        t1.ExpectApplyMigrationContent     ("_End",       Post);
        t1.ExpectDisposeConnection         ();
        t1.ExpectReportApplied             (count: 5, TargetDisposition.Successful);

        Session.DiscoverMigrations(path);
        Session.Migrations.Length.ShouldBe(5);

        Session.BeginApplying(TargetA);

        await Session.CompleteApplyingAsync(Cancellation.Token);
    }

    [Test]
    public async Task Apply_Group_OnePhase()
    {
        _options.EnabledPhases = [Pre];

        var path  = Path.Combine(TestContext.CurrentContext.TestDirectory, "TestDbs", "A");
        var group = new TargetGroup([TargetA]);

        var t = ForTarget(TargetA);
        ExpectApplyMigrations(t, Pre);

        Session.DiscoverMigrations(path);
        Session.Migrations.Length.ShouldBe(5);

        Session.BeginApplying(group);

        await Session.CompleteApplyingAsync(Cancellation.Token);
    }

    [Test]
    public async Task Apply_Group_MultiplePhases()
    {
        _options.EnabledPhases = [Pre, Post];

        var path  = Path.Combine(TestContext.CurrentContext.TestDirectory, "TestDbs", "A");
        var group = new TargetGroup([TargetA]);

        var t0 = ForTarget(TargetA);
        t0.ExpectCreateLog                 ();
        t0.ExpectReportStarting            ();
        t0.ExpectCreateAndOpenConnection   ();
        t0.ExpectGetRegisteredMigrations   ("Migration0");
        t0.ExpectInitializeMigrationSupport();
        t0.ExpectReportApplying            ("_Begin",     Pre);
        t0.ExpectApplyMigrationContent     ("_Begin",     Pre);
        t0.ExpectReportApplying            ("Migration0", Pre);
        t0.ExpectApplyMigrationContent     ("Migration0", Pre);
        t0.ExpectReportApplying            ("Migration1", Pre);
        t0.ExpectApplyMigrationContent     ("Migration1", Pre);
        t0.ExpectReportApplying            ("Migration2", Pre);
        t0.ExpectApplyMigrationContent     ("Migration2", Pre);
        t0.ExpectReportApplying            ("_End",       Pre);
        t0.ExpectApplyMigrationContent     ("_End",       Pre);
        t0.ExpectDisposeConnection         ();
        t0.ExpectReportApplied             (count: 5, TargetDisposition.Successful);

        var t1 = t0.Then(); // Reconnect for Post phase
        t1.ExpectCreateLog                 ();
        t1.ExpectReportStarting            ();
        t1.ExpectCreateAndOpenConnection   ();
        t1.ExpectGetRegisteredMigrations   ("Migration0", [
            new("Migration0") { State = MigrationState.AppliedPre },
            new("Migration1") { State = MigrationState.AppliedPre },
            new("Migration2") { State = MigrationState.AppliedPre }
        ]);
        //.ExpectInitializeMigrationSupport(); // Already initialized in Pre phase
        t1.ExpectReportApplying            ("_Begin",     Post);
        t1.ExpectApplyMigrationContent     ("_Begin",     Post);
        t1.ExpectReportApplying            ("Migration0", Post);
        t1.ExpectApplyMigrationContent     ("Migration0", Post);
        t1.ExpectReportApplying            ("Migration1", Post);
        t1.ExpectApplyMigrationContent     ("Migration1", Post);
        t1.ExpectReportApplying            ("Migration2", Post);
        t1.ExpectApplyMigrationContent     ("Migration2", Post);
        t1.ExpectReportApplying            ("_End",       Post);
        t1.ExpectApplyMigrationContent     ("_End",       Post);
        t1.ExpectDisposeConnection         ();
        t1.ExpectReportApplied             (count: 5, TargetDisposition.Successful);

        Session.DiscoverMigrations(path);
        Session.Migrations.Length.ShouldBe(5);

        Session.BeginApplying(group);

        await Session.CompleteApplyingAsync(Cancellation.Token);
    }

    [Test]
    public async Task Apply_Mix_OnePhase()
    {
        _options.EnabledPhases = [Pre];

        var path  = Path.Combine(TestContext.CurrentContext.TestDirectory, "TestDbs", "A");
        var group = new TargetGroup([TargetA, TargetB], name: "A+B", maxParallelism: 3);

        var tA = ForTarget(TargetA); ExpectApplyMigrations(tA, Pre);
        var tB = ForTarget(TargetB); ExpectApplyMigrations(tB, Pre);
        var tC = ForTarget(TargetC); ExpectApplyMigrations(tC, Pre);

        Session.DiscoverMigrations(path);
        Session.Migrations.Length.ShouldBe(5);

        Session.BeginApplying(group);
        Session.BeginApplying(TargetC);

        await Session.CompleteApplyingAsync(Cancellation.Token);
    }

    private void ExpectApplyMigrations(TargetFocus t, MigrationPhase phase)
    {
        t.ExpectCreateLog                 ();
        t.ExpectReportStarting            ();
        t.ExpectCreateAndOpenConnection   ();
        t.ExpectGetRegisteredMigrations   ("Migration0");
        t.ExpectInitializeMigrationSupport();
        t.ExpectReportApplying            ("_Begin",     phase);
        t.ExpectApplyMigrationContent     ("_Begin",     phase);
        t.ExpectReportApplying            ("Migration0", phase);
        t.ExpectApplyMigrationContent     ("Migration0", phase);
        t.ExpectReportApplying            ("Migration1", phase);
        t.ExpectApplyMigrationContent     ("Migration1", phase);
        t.ExpectReportApplying            ("Migration2", phase);
        t.ExpectApplyMigrationContent     ("Migration2", phase);
        t.ExpectReportApplying            ("_End",       phase);
        t.ExpectApplyMigrationContent     ("_End",       phase);
        t.ExpectDisposeConnection         ();
        t.ExpectReportApplied             (count: 5, TargetDisposition.Successful);
    }

    private TargetFocus ForTarget(Target target)
    {
        Session.ConnectionFactory = _factory.Object;
        return new(this, target);
    }

    private class TargetFocus
    {
        private readonly MigrationSessionTests            _parent;
        private readonly Target                           _target;
        private readonly Mock<IMigrationTargetConnection> _connection;
        private readonly MockSequence                     _sequence;
        private readonly StringWriter                     _log;

        public TargetFocus(MigrationSessionTests parent, Target target)
            : this(parent, target, sequence: new())
        { }

        public TargetFocus(TargetFocus prior)
            : this(prior._parent, prior._target, prior._sequence)
        { }

        private TargetFocus(MigrationSessionTests parent, Target target, MockSequence sequence)
        {
            _parent     = parent;
            _target     = target;
            _connection = parent.Mocks.Create<IMigrationTargetConnection>();
            _sequence   = sequence;
            _log        = new();
        }

        public MigrationSession                       Session => _parent.Session;
        public Mock<IMigrationConsole>                Console => _parent._console;
        public Mock<MigrationTargetConnectionFactory> Factory => _parent._factory;

        public string Log => _log.ToString();

        public TargetFocus Then() => new(this);

        public void ExpectCreateLog()
        {
            Console
                .InSequence(_sequence)
                .Setup(c => c.CreateLog(
                    It.Is<IMigrationApplication>(a => a.Session == Session && a.Target == _target)
                ))
                .Returns(_log)
                .Verifiable();
        }

        public void ExpectReportStarting()
        {
            Console
                .InSequence(_sequence)
                .Setup(c => c.ReportStarting(
                    It.Is<IMigrationApplication>(a => a.Session == Session && a.Target == _target)
                ))
                .Verifiable();
        }

        public void ExpectReportApplying(string name, MigrationPhase phase)
        {
            Console
                .InSequence(_sequence)
                .Setup(c => c.ReportApplying(
                    It.Is<IMigrationApplication>(a => a.Session == Session && a.Target == _target),
                    name, phase
                ))
                .Verifiable();
        }

        public void ExpectReportApplied(int count, TargetDisposition disposition)
        {
            Console
                .InSequence(_sequence)
                .Setup(c => c.ReportApplied(
                    It.Is<IMigrationApplication>(a => a.Session == Session && a.Target == _target),
                    count, It.IsAny<TimeSpan>(), disposition
                ))
                .Verifiable();
        }

        internal void ExpectReportProblem(string message)
        {
            Console
                .InSequence(_sequence)
                .Setup(c => c.ReportProblem(
                    It.Is<IMigrationApplication>(a => a.Session == Session && a.Target == _target),
                    message
                ))
                .Verifiable();
        }

        public void ExpectCreateAndOpenConnection(Exception? exceptionToThrow = null)
        {
            Factory
                .InSequence(_sequence)
                .Setup(f => f(_target, It.IsNotNull<ISqlMessageLogger>()))
                .Returns(_connection.Object)
                .Verifiable();

            var setup = _connection
                .InSequence(_sequence)
                .Setup(c => c.OpenAsync(Session.CancellationToken));

            if (exceptionToThrow is null)
                setup.Returns(Task.CompletedTask).Verifiable();
            else
                setup.Throws(exceptionToThrow).Verifiable();
        }

        public void ExpectGetRegisteredMigrations(string? minimumName, params Migration[] migrations)
        {
            _connection
                .InSequence(_sequence)
                .Setup(c => c.GetAppliedMigrationsAsync(minimumName, Session.CancellationToken))
                .ReturnsAsync(migrations)
                .Verifiable();
        }

        public void ExpectInitializeMigrationSupport()
        {
            _connection
                .InSequence(_sequence)
                .Setup(c => c.InitializeMigrationSupportAsync(Session.CancellationToken))
                .Returns(Task.CompletedTask)
                .Verifiable();
        }

        public void ExpectApplyMigrationContent(string name, MigrationPhase phase)
        {
            _connection
                .InSequence(_sequence)
                .Setup(c => c.ExecuteMigrationContentAsync(
                    It.Is<Migration>(m => m.Name == name), phase, Session.CancellationToken
                ))
                .Returns(Task.CompletedTask)
                .Verifiable();
        }

        public void ExpectDisposeConnection()
        {
            _connection
                .InSequence(_sequence)
                .Setup(c => c.DisposeAsync())
                .Returns(ValueTask.CompletedTask)
                .Verifiable();
        }
    }
}
