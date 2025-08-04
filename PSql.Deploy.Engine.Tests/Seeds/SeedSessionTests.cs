// Copyright Subatomix Research Inc.
// SPDX-License-Identifier: MIT

namespace PSql.Deploy.Seeds;

using System.Collections.Concurrent;

[TestFixture]
public class SeedSessionTests : TestHarnessBase
{
    private SeedSession? _session;

    private readonly SeedSessionOptions                _options;
    private readonly Mock<ISeedConsole>                _console;
    private readonly Mock<SeedTargetConnectionFactory> _factory;
    private readonly List<string>                      _expectedBatches;
    private readonly ConcurrentBag<string>             _actualBatches;

    private static readonly Target
        TargetA = new("Server=sql.example.com;Database=a"),
        TargetB = new("Server=sql.example.com;Database=b");

    private SeedSession Session
        => _session ??= new SeedSession(_options, _console.Object);

    private ISeedSessionInternal SessionInternal
        => Session;

    public SeedSessionTests()
    {
        _options = new() { Defines = [("foo", "bar")] };
        _console = Mocks.Create<ISeedConsole>();
        _factory = Mocks.Create<SeedTargetConnectionFactory>();

        _expectedBatches = new();
        _actualBatches   = new();
    }

    protected override void Verify()
    {
        base.Verify();

        foreach (var batch in _expectedBatches)
            _actualBatches.ShouldContain(batch);
    }

    protected override void CleanUp(bool managed)
    {
        _session?.Dispose();
        base.CleanUp(managed);
    }

    [Test]
    public void Construct_NullOptions()
    {
        Should.Throw<ArgumentNullException>(() =>
        {
            _ = new SeedSession(null!, _console.Object);
        });
    }

    [Test]
    public void Construct_NullConsole()
    {
        Should.Throw<ArgumentNullException>(() =>
        {
            _ = new SeedSession(_options, null!);
        });
    }

    [Test]
    public void Console_Get()
    {
        Session.Console.ShouldBeSameAs(_console.Object);
    }

    [Test]
    public void Defines_Get()
    {
        Session.Defines.ShouldBeSameAs(_options.Defines);
    }

    [Test]
    public void Defines_Get_Default()
    {
        _options.Defines = null;

        Session.Defines.ShouldNotBeNull().ShouldBeEmpty();
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
    public void Seeds_Get_Initial()
    {
        Session.Seeds.ShouldBeEmpty();
    }

    [Test]
    public void DiscoverSeeds_NullPath()
    {
        Should.Throw<ArgumentNullException>(() =>
        {
            SessionInternal.DiscoverSeeds(null!, ["some-seed"]);
        });
    }

    [Test]
    public void DiscoverSeeds_NullNames()
    {
        Should.Throw<ArgumentNullException>(() =>
        {
            _ = SeedDiscoverer.Get("path", null!);
        });
    }

    [Test]
    public void DiscoverSeeds_NullName()
    {
        Should.Throw<ArgumentException>(() =>
        {
            _ = SeedDiscoverer.Get("path", [null!]);
        });
    }

    [Test]
    public void DiscoverSeeds_NotFound()
    {
        Should.Throw<FileNotFoundException>(() =>
        {
            _ = SeedDiscoverer.Get("nonexistent-path", ["nonexistent-seed"]);
        });
    }

    [Test]
    public void DiscoverSeeds_Ok()
    {
        var path = Path.Combine(
            TestContext.CurrentContext.TestDirectory, "TestDbs", "A"
        );

        var seeds = SeedDiscoverer.Get(path, ["Typical"]);

        seeds.ShouldHaveSingleItem().AssignTo(out var seed);

        seed.Name.ShouldBe("Typical");
        seed.Path.ShouldBe(Path.Combine(path, "Seeds", "Typical", "_Main.sql"));
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

        connection       .ShouldBeOfType<SqlSeedTargetConnection>();
        connection.Target.ShouldBeSameAs(TargetA);
        connection.Logger.ShouldBeSameAs(logger.Object);
    }

    [Test]
    public void Connect_WhatIf()
    {
        _options.IsWhatIfMode = true;

        var logger = Mocks.Create<ISqlMessageLogger>();

        using var connection = SessionInternal.Connect(TargetA, logger.Object);

        connection       .ShouldBeOfType<WhatIfSeedTargetConnection>();
        connection.Target.ShouldBeSameAs(TargetA);
        connection.Logger.ShouldBeSameAs(logger.Object);
    }

    [Test]
    public async Task Apply_Target_Exception()
    {
        var path = Path.Combine(TestContext.CurrentContext.TestDirectory, "TestDbs", "A");

        var innerException = new InvalidOperationException("Test exception.");

        var t = ForTarget(TargetA);
        t.ExpectCreateLog("Typical");
        t.ExpectReportStarting();
        t.ExpectUseConnection(innerException);
        t.ExpectReportProblem("Test exception.");

        Session.DiscoverSeeds(path, ["Typical"]);
        Session.BeginApplying(TargetA, maxParallelism: 1);

        var outerException = await Should.ThrowAsync<SeedException>(() =>
        {
            return Session.CompleteApplyingAsync(Cancellation.Token);
        });

        outerException.InnerException.ShouldBeSameAs(innerException);
    }

    [Test]
    public async Task Apply_Target_Ok()
    {
        var path = Path.Combine(TestContext.CurrentContext.TestDirectory, "TestDbs", "A");

        ForTarget(TargetA).ExpectApplyTypicalSeed();

        Session.DiscoverSeeds(path, ["Typical"]);
        Session.BeginApplying(TargetA, maxParallelism: 1);

        await Session.CompleteApplyingAsync(Cancellation.Token);
    }

    [Test]
    public async Task Apply_Group_Ok()
    {
        var path = Path.Combine(TestContext.CurrentContext.TestDirectory, "TestDbs", "A");

        var group = new TargetGroup(
            [TargetA, TargetB],
            maxParallelism:          1,
            maxParallelismPerTarget: 1
        );

        ForTarget(TargetA).ExpectApplyTypicalSeed();
        ForTarget(TargetB).ExpectApplyTypicalSeed();

        Session.DiscoverSeeds(path, ["Typical"]);
        Session.BeginApplying(group);

        await Session.CompleteApplyingAsync(Cancellation.Token);
    }

    private TargetFocus ForTarget(Target target)
    {
        Session.ConnectionFactory = _factory.Object;
        return new(this, target);
    }

    private class TargetFocus
    {
        private readonly SeedSessionTests                  _parent;
        private readonly Target                            _target;
        private readonly List<Mock<ISeedTargetConnection>> _connections;
        private readonly MockSequence                      _sequence;
        private readonly StringWriter                      _log;

        public TargetFocus(SeedSessionTests parent, Target target)
        {
            _parent      = parent;
            _target      = target;
            _connections = new();
            _sequence    = new();
            _log         = new();
        }

        public SeedSession                       Session => _parent.Session;
        public Mock<ISeedConsole>                Console => _parent._console;
        public Mock<SeedTargetConnectionFactory> Factory => _parent._factory;

        public string Log => _log.ToString();

        public void ExpectApplyTypicalSeed()
        {
            ExpectCreateLog("Typical");
            ExpectReportStarting();
            ExpectUseConnection();
            ExpectReportApplying("(init)");
            ExpectInvokeBatch(TypicalSeed_InitialModule_Batch0);
            ExpectReportApplying("a");
            ExpectInvokeBatch(TypicalSeed_ModuleA_Batch0);
            ExpectReportApplying("b");
            ExpectInvokeBatch(TypicalSeed_ModuleB_Batch0);
        }

        public void ExpectCreateLog(string seedName)
        {
            Console
                .InSequence(_sequence)
                .Setup(c => c.CreateLog(
                    It.Is<ISeedApplication>(a
                        => a.Target         == _target
                        && a.Seed.Seed.Name == seedName
                    )
                ))
                .Returns(_log)
                .Verifiable();
        }

        public void ExpectReportStarting()
        {
            Console
                .InSequence(_sequence)
                .Setup(c => c.ReportStarting(
                    It.Is<ISeedApplication>(a => a.Target == _target)
                ))
                .Verifiable();
        }

        internal void ExpectReportApplying(string moduleName)
        {
            Console
                .Setup(c => c.ReportApplying(
                    It.Is<ISeedApplication>(a => a.Target == _target),
                    moduleName
                ))
                .Verifiable();
        }

        internal void ExpectReportProblem(string message)
        {
            Console
                .InSequence(_sequence)
                .Setup(c => c.ReportProblem(
                    It.Is<ISeedApplication>(a => a.Target == _target),
                    message
                ))
                .Verifiable();
        }

        public void ExpectUseConnection(Exception? exceptionToThrow = null)
        {
            var connection = _parent.Mocks.Create<ISeedTargetConnection>();
            var sequence   = new MockSequence();

            Factory
                .InSequence(sequence)
                .Setup(f => f(_target, It.IsNotNull<ISqlMessageLogger>()))
                .Returns(connection.Object)
                .Verifiable();

            var setup = connection
                .InSequence(sequence)
                .Setup(c => c.OpenAsync(Session.CancellationToken));

            if (exceptionToThrow is not null)
            {
                setup.Throws(exceptionToThrow).Verifiable();
            }
            else
            {
                setup.Returns(Task.CompletedTask).Verifiable();

                connection
                    .InSequence(sequence)
                    .Setup(c => c.PrepareAsync(
                        It.Is<Guid>(runId => runId != Guid.Empty),
                        It.Is<int>(workerId => workerId > 0),
                        Session.CancellationToken
                    ))
                    .Returns(Task.CompletedTask)
                    .Verifiable();
            }

            connection
                .InSequence(sequence)
                .Setup(c => c.DisposeAsync())
                .Returns(ValueTask.CompletedTask)
                .Verifiable();

            _connections.Add(connection);
        }

        internal void ExpectInvokeBatch(string sql)
        {
            _parent._expectedBatches.Add(sql);

            foreach (var connection in _connections)
            {
                // This setup cannot be .Verifiable() because only one
                // connection will execute a particular batch, and which
                // connection executes it is nondeterministic.
                connection
                    .Setup(c => c.ExecuteSeedBatchAsync(sql, It.IsAny<CancellationToken>()))
                    .Callback(() => _parent._actualBatches.Add(sql))
                    .Returns(Task.CompletedTask);
            }
        }
    }

    // PSql.Deploy tests use CRLF line endings in SQL files so that file
    // content is stable across platforms.
    private const string Eol = "\r\n";

    private const string
        TypicalSeed_InitialModule_Batch0
            = "PRINT 'This is in the initial module.';" + Eol,
        TypicalSeed_ModuleA_Batch0
            = "--# PROVIDES: x y"                       + Eol
            + "--# provides: y x"                       + Eol
            + "--# Provides:"                           + Eol
            + "PRINT 'This is in module a.';"           + Eol
            + "PRINT 'The value of ''foo'' is bar.';"   + Eol,
        TypicalSeed_ModuleB_Batch0
            = "--# REQUIRES:  x  y"                     + Eol
            + "--# requires:  y  x"                     + Eol
            + "--# Requires:  "                         + Eol
            + "PRINT 'This is in module b.';"           + Eol;
}
