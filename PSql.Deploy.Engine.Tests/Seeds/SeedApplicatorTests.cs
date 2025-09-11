// Copyright Subatomix Research Inc.
// SPDX-License-Identifier: MIT

namespace PSql.Deploy.Seeds;

[TestFixture]
public class SeedApplicatorTests : TestHarnessBase
{
    private SeedApplicator? _applicator;
    private LoadedSeed      _seed;

    private readonly Mock<ISeedSessionInternal>  _session;
    private readonly Mock<ISeedConsole>          _console;
    private readonly Mock<ISeedTargetConnection> _connection;
    private readonly MockSequence                _sequence;
    private readonly Target                      _target;
    private readonly TargetParallelism           _parallelism;
    private readonly StringWriter                _log;

    public SeedApplicatorTests()
    {
        _target       = new("Server=db.example.com;Database=test;User ID=test;Password=test");
        _parallelism  = new(NullParallelismLimiter.Instance, maxActions: 1);
        _session      = Mocks.Create<ISeedSessionInternal>();
        _console      = Mocks.Create<ISeedConsole>();
        _connection   = Mocks.Create<ISeedTargetConnection>();
        _sequence     = new();
        _log          = new();

        _seed = MakeSeed(
            "Test",
            MakeModule("init", ["init-sql"]),
            MakeModule("A", ["a-sql"], provides: ["X"]),
            MakeModule("B", ["b-sql"], requires: ["X"])
        );

        _session
            .Setup(s => s.Console)
            .Returns(_console.Object);
        _session
            .Setup(s => s.CancellationToken)
            .Returns(Cancellation.Token);
    }

    private SeedApplicator Applicator
        => _applicator ??= new(_session.Object, _seed, _target, _parallelism);

    [Test]
    public void Construct_NullSession()
    {
        Should.Throw<ArgumentNullException>(() =>
        {
            _ = new SeedApplicator(null!, _seed, _target, _parallelism);
        });
    }

    [Test]
    public void Construct_NullSeed()
    {
        Should.Throw<ArgumentNullException>(() =>
        {
            _ = new SeedApplicator(_session.Object, null!, _target, _parallelism);
        });
    }

    [Test]
    public void Construct_NullTarget()
    {
        Should.Throw<ArgumentNullException>(() =>
        {
            _ = new SeedApplicator(_session.Object, _seed, null!, _parallelism);
        });
    }

    [Test]
    public void Construct_NullParallelism()
    {
        Should.Throw<ArgumentNullException>(() =>
        {
            _ = new SeedApplicator(_session.Object, _seed, _target, null!);
        });
    }

    [Test]
    public void Session_Get()
    {
        Applicator.Session.ShouldBeSameAs(_session.Object);
    }

    [Test]
    public void ISeedApplication_Session_Get()
    {
        ((ISeedApplication) Applicator).Session.ShouldBeSameAs(_session.Object);
    }

    [Test]
    public void Console_Get()
    {
        Applicator.Console.ShouldBeSameAs(_console.Object);
    }

    [Test]
    public void Seed_Get()
    {
        Applicator.Seed.ShouldBeSameAs(_seed);
    }

    [Test]
    public void Target_Get()
    {
        Applicator.Target.ShouldBeSameAs(_target);
    }

    [Test]
    public void Parallelism_Get()
    {
        Applicator.Parallelism.ShouldBeSameAs(_parallelism);
    }

    [Test]
    public async Task ApplyAsync_Ok()
    {
        ExpectCreateLog();
        ExpectReportStarting();
        ExpectConnect();
        ExpectPrepare();
        ExpectReportApplying("init"); ExpectExecuteBatch("init-sql");
        ExpectReportApplying("A");    ExpectExecuteBatch("a-sql");
        ExpectReportApplying("B");    ExpectExecuteBatch("b-sql");
        ExpectReportApplied(count: 3, TargetDisposition.Successful);

        await Applicator.ApplyAsync();

        LogShouldContainAll(
            "PSql.Deploy Seed Log",
            "Seed Modules: 3",
            "The seed is valid.",
            "Applied 3 modules(s)"
        );
    }

    [Test]
    public async Task ApplyAsync_Cycle()
    {
        _seed = MakeSeed(
            "Test",
            MakeModule("init", ["init-sql"]),
            MakeModule("A", ["a-sql"], requires: ["B"]), // \_ cyclic
            MakeModule("B", ["b-sql"], requires: ["A"])  // /  dependency
        );

        ExpectCreateLog();
        ExpectReportStarting();
        ExpectReportProblem("The dependency graph does not permit cycles.");
        ExpectReportProblem("The seed is invalid.");
        ExpectReportApplied(count: 0, TargetDisposition.Failed);

        await Should.ThrowAsync<SeedException>(Applicator.ApplyAsync);

        LogShouldContainAll(
            "PSql.Deploy Seed Log",
            "Seed Modules: 3",
            "The dependency graph does not permit cycles.",
            "Applied 0 modules(s)"
        );
    }

    [Test]
    public async Task ApplyAsync_UnprovidedTopic()
    {
        _seed = MakeSeed(
            "Test",
            MakeModule("init", ["init-sql"]),
            MakeModule("A", ["a-sql"], requires: ["X"]), // \_ unprovided
            MakeModule("B", ["b-sql"], requires: ["X"])  // /  topic
        );

        ExpectCreateLog();
        ExpectReportStarting();
        ExpectReportProblem("The topic 'X' is required but not provided by any module.");
        ExpectReportProblem("The seed is invalid.");
        ExpectReportApplied(count: 0, TargetDisposition.Failed);

        await Should.ThrowAsync<SeedException>(Applicator.ApplyAsync);

        LogShouldContainAll(
            "PSql.Deploy Seed Log",
            "Seed Modules: 3",
            "The topic 'X' is required but not provided by any module.",
            "Applied 0 modules(s)"
        );
    }

    [Test]
    public async Task ApplyAsync_Canceled()
    {
        ExpectCreateLog();
        ExpectReportStarting();
        ExpectConnectCanceled();
        ExpectReportApplied(count: 0, TargetDisposition.Incomplete);

        await Should.ThrowAsync<OperationCanceledException>(Applicator.ApplyAsync);

        LogShouldContainAll(
            "PSql.Deploy Seed Log",
            "Seed Modules: 3",
            "Seed application was canceled.",
            "Applied 0 modules(s)"
        );
    }

    [Test]
    public async Task ApplyAsync_Exception()
    {
        var e = new DataException("Database is on fire.");

        ExpectCreateLog();
        ExpectReportStarting();
        ExpectConnect();
        ExpectPrepare();
        ExpectReportApplying("init"); ExpectExecuteBatch("init-sql");
        ExpectReportApplying("A");    ExpectExecuteBatch("a-sql", exception: e);
        ExpectReportProblem("Database is on fire.");
        ExpectReportApplied(count: 1, TargetDisposition.Failed);

        await Should.ThrowAsync<DataException>(Applicator.ApplyAsync);

        LogShouldContainAll(
            "PSql.Deploy Seed Log",
            "Seed Modules: 3",
            "System.Data.DataException: Database is on fire.",
            "Applied 1 modules(s)"
        );
    }

    private static LoadedSeed MakeSeed(string name, params SeedModule[] modules)
    {
        return new(
            seed: new(name, Path.Combine(".", name, "_Main.sql")),
            modules.ToImmutableArray()
        );
    }

    private static SeedModule MakeModule(
        string    name,
        string[]  batches,
        string[]? provides   = null,
        string[]? requires   = null,
        bool      allWorkers = false)
    {
        provides ??= [];
        requires ??= [];

        return new(
            name,
            allWorkers ? -1 : 0, // workerId
            batches .ToImmutableArray(),
            provides.ToImmutableArray(),
            requires.ToImmutableArray()
        );
    }

    private void ExpectCreateLog()
    {
        _console
            .InSequence(_sequence)
            .Setup(s => s.CreateLog(Applicator))
            .Returns(_log)
            .Verifiable();
    }

    private void ExpectReportStarting()
    {
        _console
            .InSequence(_sequence)
            .Setup(c => c.ReportStarting(Applicator))
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

    private void ExpectPrepare()
    {
        _connection
            .InSequence(_sequence)
            .Setup(c => c.PrepareAsync(
                It.Is<Guid>(runId => runId != Guid.Empty),
                /*workerId*/ 1, Cancellation.Token
            ))
            .Returns(Task.CompletedTask)
            .Verifiable();
    }

    private void ExpectReportApplying(string moduleName)
    {
        _console
            .InSequence(_sequence)
            .Setup(c => c.ReportApplying(Applicator, moduleName))
            .Verifiable();
    }

    private void ExpectExecuteBatch(string sql, Exception? exception = null)
    {
        var expectation = _connection
            .InSequence(_sequence)
            .Setup(c => c.ExecuteSeedBatchAsync(sql, It.Is<CancellationToken>(t => t != default)));

        if (exception is not null)
            expectation.ThrowsAsync(exception).Verifiable();
        else
            expectation.Returns(Task.CompletedTask).Verifiable();
    }

    private void ExpectReportApplied(int count, TargetDisposition disposition)
    {
        _console
            .InSequence(_sequence)
            .Setup(c => c.ReportApplied(
                Applicator, count, It.Is<TimeSpan>(t => t >= TimeSpan.Zero), disposition
            ))
            .Verifiable();
    }

    private void ExpectReportProblem(string part)
    {
        _console
            .InSequence(_sequence)
            .Setup(c => c.ReportProblem(
                Applicator, It.Is<string>(s => s.Contains(part, StringComparison.Ordinal))
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
