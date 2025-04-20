// Copyright Subatomix Research Inc.
// SPDX-License-Identifier: MIT

using Moq.Protected;

namespace PSql.Deploy.Migrations;

[TestFixture]
public class MigrationApplicatorTests : TestHarnessBase
{
    private readonly MigrationApplicator             _applicator;
    private readonly Mock<IMigrationSessionInternal> _session;
    private readonly Mock<IMigrationConsole>         _console;
    private readonly Target                          _target;
    private readonly StringWriter                    _log;

    public MigrationApplicatorTests()
    {
        _target  = new("Server=db.example.com;Database=test;User ID=test;Password=test");
        _session = Mocks.Create<IMigrationSessionInternal>();
        _console = Mocks.Create<IMigrationConsole>();
        _log     = new StringWriter();

        _session
            .Setup(s => s.AllowContentInCorePhase)
            .Returns(false);
        _session
            .Setup(s => s.IsWhatIfMode)
            .Returns(false);
        _session
            .Setup(s => s.CurrentPhase)
            .Returns(MigrationPhase.Pre);
        _session
            .Setup(s => s.Console)
            .Returns(_console.Object);
        //_session
        //    .Setup(s => s.CreateLog(_work))
        //    .Returns(_log);

        _applicator = new MigrationApplicator(_session.Object, _target);
    }

    [Test]
    public void Construct_NullSession()
    {
        Should.Throw<ArgumentNullException>(() =>
        {
            new MigrationApplicator(null!, _target);
        });
    }

    [Test]
    public void Construct_NullContext()
    {
        Should.Throw<ArgumentNullException>(() =>
        {
            new MigrationApplicator(_session.Object, null!);
        });
    }

#if CONVERTED

    [Test]
    public void Session_Get()
    {
        _applicator.Session.ShouldBeSameAs(_session.Object);
    }

    [Test]
    public void Context_Get()
    {
        _applicator.Context.ShouldBeSameAs(_target.Context);
    }

    [Test]
    public void EarliestDefinedMigrationName_Get()
    {
        var value = "a";

        _session.Setup(s => s.EarliestDefinedMigrationName).Returns(value);

        _applicator.EarliestDefinedMigrationName.ShouldBeSameAs(value);
    }

    [Test]
    public void Phase_Get()
    {
        var value = MigrationPhase.Core;

        _session.Setup(s => s.Phase).Returns(value);

        _applicator.Phase.ShouldBe(value);
    }

    [Test]
    public void CancellationToken_Get()
    {
        using var cancellation = new CancellationTokenSource();

        _session.Setup(s => s.CancellationToken).Returns(cancellation.Token);

        _applicator.CancellationToken.ShouldBe(cancellation.Token);
    }

    [Test]
    public void ServerName_Get()
    {
        _applicator.ServerName.ShouldBe(_target.ServerDisplayName);
    }

    [Test]
    public void DatabaseName_Get()
    {
        _applicator.DatabaseName.ShouldBe(_target.DatabaseDisplayName);
    }

    [Test]
    public void LogWriter_Get()
    {
        _applicator.LogWriter.ShouldBeSameAs(_log);
    }

    [Test]
    public void LogConsole_Get()
    {
        _applicator.SqlMessageLogger.ShouldBeOfType<TextWriterSqlMessageLogger>();
    }

    [Test]
    public void AllowCorePhase_Get()
    {
        _applicator.AllowCorePhase.ShouldBeFalse();
    }

    [Test]
    public void IsWhatIfMode_Get()
    {
        _applicator.IsWhatIfMode.ShouldBeFalse();
    }

    [Test]
    public async Task ApplyAsync_NoPendingMigrations()
    {
        _console
            .Setup(c => c.ReportStarting())
            .Verifiable();

        _session
            .Setup(s => s.Migrations)
            .Returns(ImmutableArray<Migration>.Empty)
            .Verifiable();

        _session
            .Setup(s => s.GetAppliedMigrationsAsync(_applicator.Context, _applicator.SqlMessageLogger))
            .ReturnsAsync(new Migration[0])
            .Verifiable();

        _console
            .Setup(c => c.ReportApplied(
                0, It.Is<TimeSpan>(t => t >= TimeSpan.Zero),
                TargetDisposition.Successful
            ))
            .Verifiable();

        await _applicator.ApplyAsync();

        _log.ToString().ShouldContain("Nothing to do.");
    }

    [Test]
    public async Task ApplyAsync_Invalid()
    {
        var a = new Migration("a")
        {
            Path      = "/test/a",
            DependsOn = ImmutableArray.Create(new MigrationReference("a"))
        };

        _console
            .Setup(c => c.ReportStarting())
            .Verifiable();

        _session
            .Setup(s => s.Migrations)
            .Returns(ImmutableArray.Create(a))
            .Verifiable();

        _session
            .Setup(s => s.GetAppliedMigrationsAsync(_applicator.Context, _applicator.SqlMessageLogger))
            .ReturnsAsync(new Migration[0])
            .Verifiable();

        _internals
            .Setup(i => i.LoadContent(a))
            .Callback(() => { a.IsContentLoaded = true; })
            .Verifiable();

        _console
            .Setup(c => c.ReportProblem(
                "Migration 'a' declares a dependency on itself. " +
                "The dependency cannot be satisfied."
            ))
            .Verifiable();

        _console
            .Setup(c => c.ReportApplied(
                0, It.Is<TimeSpan>(t => t >= TimeSpan.Zero),
                TargetDisposition.Successful // TODO: really?
            ))
            .Verifiable();

        await _applicator.ApplyAsync();

        _log.ToString().ShouldContainAll(
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

        _console
            .Setup(c => c.ReportStarting())
            .Verifiable();

        _session
            .Setup(s => s.Migrations)
            .Returns(ImmutableArray.Create(aDefined))
            .Verifiable();

        _session
            .Setup(s => s.GetAppliedMigrationsAsync(_applicator.Context, _applicator.SqlMessageLogger))
            .ReturnsAsync(new[] { aApplied })
            .Verifiable();

        _internals
            .Setup(i => i.LoadContent(aDefined))
            .Callback(() => { aDefined.IsContentLoaded = true; })
            .Verifiable();

        _console
            .Setup(c => c.ReportApplied(
                0, It.Is<TimeSpan>(t => t >= TimeSpan.Zero),
                TargetDisposition.Successful
            ))
            .Verifiable();

        await _applicator.ApplyAsync();

        _log.ToString().ShouldContainAll(
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

        _console
            .Setup(c => c.ReportStarting())
            .Verifiable();

        _session
            .Setup(s => s.Migrations)
            .Returns(ImmutableArray.Create(aDefined))
            .Verifiable();

        _session
            .Setup(s => s.GetAppliedMigrationsAsync(_applicator.Context, _applicator.SqlMessageLogger))
            .ReturnsAsync(new[] { aApplied })
            .Verifiable();

        _internals
            .Setup(i => i.LoadContent(aDefined))
            .Callback(() => { aDefined.IsContentLoaded = true; })
            .Verifiable();

        _console
            .Setup(c => c.ReportProblem(
                "One or more migration(s) to be applied to database [db.example.com].[test] "   +
                "requires the Core (downtime) phase, but the -AllowCorePhase switch was not "   +
                "present for the Invoke-SqlMigrations command.  To allow the Core phase, pass " +
                "the switch to the command.  Otherwise, ensure that all migrations begin with " +
                "a '--# PRE' or '--# POST' directive and that any '--# REQUIRES:' directives "  +
                "reference only migrations that have been completely applied."
            ))
            .Verifiable();

        _console
            .Setup(c => c.ReportApplied(
                0, It.Is<TimeSpan>(t => t >= TimeSpan.Zero),
                TargetDisposition.Successful // TODO: Really?
            ))
            .Verifiable();

        await _applicator.ApplyAsync();

        _log.ToString().ShouldContainAll(
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

        _session
            .Setup(s => s.AllowContentInCorePhase)
            .Returns(true);

        _session
            .Setup(s => s.CancellationToken)
            .Returns(cancellation.Token);

        _console
            .Setup(c => c.ReportStarting(_target))
            .Verifiable();

        _session
            .Setup(s => s.Migrations)
            .Returns(ImmutableArray.Create<IMigration>(a))
            .Verifiable();

        _session
            .Setup(s => s.GetAppliedMigrationsAsync(_target))
            .ReturnsAsync([])
            .Verifiable();

        _session
            .Setup(i => i.LoadContent(a))
            .Verifiable();

        var connection = Mocks.Create<SqlConnection>();
        var command    = Mocks.Create<SqlCommand>();
        var command2   = Mocks.Create<DbCommand>();

        //_internals
        //    .Setup(i => i.Connect(_target.Context, _applicator.SqlMessageLogger))
        //    .Returns(connection.Object)
        //    .Verifiable();

        connection
            .Setup(c => c.CreateCommand())
            .Returns(command.Object)
            .Verifiable();

        command
            .SetupSet(c => c.CommandTimeout = 0)
            .Verifiable();

        _console
            .Setup(c => c.ReportApplying(Target, "a", MigrationPhase.Pre))
            .Verifiable();

        //connection
        //    .Setup(c => c.ClearErrors())
        //    .Verifiable();

        command
            .SetupSet(c => c.CommandText = It.IsRegex("pre-sql"))
            .Verifiable();

        //command
        //    .Setup(c => c.UnderlyingCommand)
        //    .Returns(command2.Object);

        command2
            .Setup(c => c.ExecuteNonQueryAsync(_session.CancellationToken))
            .ReturnsAsync(0)
            .Verifiable();

        //connection
        //    .Setup(c => c.ThrowIfHasErrors())
        //    .Verifiable();

        command2
            .Protected().Setup("Dispose", ItExpr.IsAny<bool>());
            // Prevent spurious exception if GC collects command2

        command
            .Setup(c => c.Dispose())
            .Verifiable();

        connection
            .Setup(c => c.Dispose())
            .Verifiable();

        _console
            .Setup(c => c.ReportApplied(
                _target, 1, It.Is<TimeSpan>(t => t >= TimeSpan.Zero),
                TargetDisposition.Successful
            ))
            .Verifiable();

        await _applicator.ApplyAsync();

        _log.ToString().ShouldSatisfyAllConditions(
            s => s.ShouldContain("PSql.Deploy Migration Log"),
            s => s.ShouldContain("Migration Phase:    Pre"),
            s => s.ShouldContain("Pending Migrations: 1"),
            s => s.ShouldContain("All pending migrations are valid for the current phase."),
            s => s.ShouldContain("Applied 1 migration(s)")
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

        _console
            .Setup(c => c.ReportStarting())
            .Verifiable();

        _session
            .Setup(s => s.Migrations)
            .Returns(ImmutableArray.Create(a))
            .Verifiable();

        _session
            .Setup(s => s.GetAppliedMigrationsAsync(_applicator.Context, _applicator.SqlMessageLogger))
            .ReturnsAsync(new Migration[0])
            .Verifiable();

        _internals
            .Setup(i => i.LoadContent(a))
            .Verifiable();

        var connection = Mocks.Create<ISqlConnection>();
        var command    = Mocks.Create<ISqlCommand>();

        _internals
            .Setup(i => i.Connect(_target.Context, _applicator.SqlMessageLogger))
            .Returns(connection.Object)
            .Verifiable();

        connection
            .Setup(c => c.CreateCommand())
            .Returns(command.Object)
            .Verifiable();

        command
            .SetupSet(c => c.CommandTimeout = 0)
            .Verifiable();

        _console
            .Setup(c => c.ReportApplying("a", MigrationPhase.Pre))
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

        _console
            .Setup(c => c.ReportApplied(
                1, It.Is<TimeSpan>(t => t >= TimeSpan.Zero),
                TargetDisposition.Successful
            ))
            .Verifiable();

        await _applicator.ApplyAsync();

        _log.ToString().ShouldContainAll(
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

        _session
            .Setup(s => s.IsWhatIfMode)
            .Returns(true);

        _session
            .Setup(s => s.CancellationToken)
            .Returns(cancellation.Token);

        _console
            .Setup(c => c.ReportStarting())
            .Verifiable();

        _session
            .Setup(s => s.Migrations)
            .Returns(ImmutableArray.Create(a))
            .Verifiable();

        _session
            .Setup(s => s.GetAppliedMigrationsAsync(_applicator.Context, _applicator.SqlMessageLogger))
            .ReturnsAsync(new Migration[0])
            .Verifiable();

        _internals
            .Setup(i => i.LoadContent(a))
            .Verifiable();

        // TODO: Should this have happened?
        //_console
        //    .Setup(c => c.ReportApplying(MigrationPhase.Pre))
        //    .Verifiable();

        _console
            .Setup(c => c.ReportApplied(
                0, It.Is<TimeSpan>(t => t >= TimeSpan.Zero),
                TargetDisposition.Successful
            ))
            .Verifiable();

        await _applicator.ApplyAsync();

        _log.ToString().ShouldContainAll(
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

        _console
            .Setup(c => c.ReportStarting())
            .Verifiable();

        _session
            .Setup(s => s.Migrations)
            .Returns(ImmutableArray.Create(a))
            .Verifiable();

        _session
            .Setup(s => s.GetAppliedMigrationsAsync(_applicator.Context, _applicator.SqlMessageLogger))
            .ReturnsAsync(new Migration[0])
            .Verifiable();

        _internals
            .Setup(i => i.LoadContent(a))
            .Verifiable();

        _internals
            .Setup(i => i.Connect(_target.Context, _applicator.SqlMessageLogger))
            .Throws(new Exception("Oops!"));

        _console
            .Setup(r => r.ReportProblem("Oops!"))
            .Verifiable();

        _console
            .Setup(c => c.ReportApplied(
                0, It.Is<TimeSpan>(t => t >= TimeSpan.Zero),
                TargetDisposition.Failed
            ))
            .Verifiable();

        await _applicator.Awaiting(t => t.ApplyAsync())
            .ShouldThrowAsync<Exception>().WithMessage("Oops!");

        _log.ToString().ShouldContainAll(
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

        _console
            .Setup(c => c.ReportStarting())
            .Verifiable();

        _session
            .Setup(s => s.Migrations)
            .Returns(ImmutableArray.Create(a))
            .Verifiable();

        _session
            .Setup(s => s.GetAppliedMigrationsAsync(_applicator.Context, _applicator.SqlMessageLogger))
            .ReturnsAsync(new Migration[0])
            .Verifiable();

        _internals
            .Setup(i => i.LoadContent(a))
            .Verifiable();

        _internals
            .Setup(i => i.Connect(_target.Context, _applicator.SqlMessageLogger))
            .Throws(new OperationCanceledException());

        _console
            .Setup(c => c.ReportApplied(
                0, It.Is<TimeSpan>(t => t >= TimeSpan.Zero),
                TargetDisposition.Incomplete
            ))
            .Verifiable();

        await _applicator.Awaiting(t => t.ApplyAsync())
            .ShouldThrowAsync<OperationCanceledException>();

        _log.ToString().ShouldContainAll(
            "PSql.Deploy Migration Log",
            "Migration Phase:    Pre",
            "Pending Migrations: 1",
            "All pending migrations are valid for the current phase.",
            "Applied 0 migration(s)"
        );
    }
#endif // CONVERTED
}
