// Copyright Subatomix Research Inc.
// SPDX-License-Identifier: MIT

namespace PSql.Deploy.Migrations;

[TestFixture]
public class WhatIfMigrationTargetConnectionTests : TestHarnessBase
{
    private readonly WhatIfMigrationTargetConnection  _outer;
    private readonly Mock<IMigrationTargetConnection> _inner;
    private readonly Mock<ISqlMessageLogger>          _logger;
    private readonly Target                           _target;
    private readonly WhatIfMigrationState             _state;

    public WhatIfMigrationTargetConnectionTests()
    {
        _inner  = Mocks.Create<IMigrationTargetConnection>();
        _logger = Mocks.Create<ISqlMessageLogger>();
        _target = new("Server=.;Database=db");
        _state  = new();

        _inner.Setup(c => c.Target).Returns(_target);
        _inner.Setup(c => c.Logger).Returns(_logger.Object);

        _outer = new(_inner.Object, _state);
    }

    [Test]
    public void Construct_NullConnection()
    {
        Should.Throw<ArgumentNullException>(() =>
        {
            _ = new WhatIfMigrationTargetConnection(null!, _state);
        });
    }

    [Test]
    public void Construct_NullState()
    {
        Should.Throw<ArgumentNullException>(() =>
        {
            _ = new WhatIfMigrationTargetConnection(_inner.Object, null!);
        });
    }

    [Test]
    public async Task GetAppliedMigrationsAsync()
    {
        // Migration 1 has been 'what-if' applied in the pre phase
        _state.OnApplied(_target, new("M1"), MigrationPhase.Pre);

        // Migrations have not been applied according to target database
        var minimumName = "M0";
        var migration1  = new Migration("M1") { State = MigrationState.NotApplied };
        var migration2  = new Migration("M2") { State = MigrationState.NotApplied };

        _inner
            .Setup(c => c.GetAppliedMigrationsAsync(minimumName, Cancellation.Token))
            .ReturnsAsync([migration1, migration2])
            .Verifiable();

        var result = await _outer.GetAppliedMigrationsAsync(minimumName, Cancellation.Token);

        result.ShouldBe([migration1, migration2]);

        // What-if state overrides the actual state
        migration1.State.ShouldBe(MigrationState.AppliedPre); // overridden
        migration2.State.ShouldBe(MigrationState.NotApplied);
    }

    [Test]
    public async Task InitializeMigrationSupportAsync()
    {
        _logger.Setup(l => l.Log("", 0, 0, 0, "Would initialize migration support."));

        await _outer.InitializeMigrationSupportAsync(Cancellation.Token);
    }

    [Test]
    public async Task ExecuteMigrationContentAsync_NullMigration()
    {
        await Should.ThrowAsync<ArgumentNullException>(() =>
        {
            return _outer.ExecuteMigrationContentAsync(null!, MigrationPhase.Core, Cancellation.Token);
        });
    }

    [Test]
    public async Task ExecuteMigrationContentAsync_Ok()
    {
        // Migration has been applied through Core phase according to target database
        var migration = new Migration("M3") { State = MigrationState.AppliedCore };

        _logger.Setup(l => l.Log("", 0, 0, 0, "Would execute migration 'M3' Post content."));

        await _outer.ExecuteMigrationContentAsync(migration, MigrationPhase.Post, Cancellation.Token);

        // ExecuteMigrationContentAsync did *NOT* update the migration.State
        // property; only GetAppliedMigrationsAsync does that.
        migration.State.ShouldBe(MigrationState.AppliedCore);

        // Instead, ExecuteMigrationContentAsync updated the what-if state,
        // which a later GetAppliedMigrationsAsync would consume.
        _state.Get(_target, [migration])
            .ShouldHaveSingleItem()
            .State.ShouldBe(MigrationState.AppliedPost);
    }
}
