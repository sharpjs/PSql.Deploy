// Copyright Subatomix Research Inc.
// SPDX-License-Identifier: MIT

using PSql.Deploy.Migrations;

namespace PSql.Deploy.Integration.Migrations;

[TestFixture]
public class SqlMigrationTargetConnectionIntegrationTests
{
    private readonly SqlMigrationTargetConnection _connection;

    public SqlMigrationTargetConnectionIntegrationTests()
    {
        _connection = new SqlMigrationTargetConnection(
            IntegrationTestsSetup.Target, new TestSqlLogger()
        );
    }

    [Test]
    public async Task Test()
    {
        var cancellation = CancellationToken.None;

        var migration = new Migration("Test1")
        {
            Pre =
            {
                Sql =
                """
                INSERT _deploy.Migration (Name, Hash, PreRunDate)
                VALUES (N'Test1', N'F3BBBD66A63D4BF1747940578EC3D0103530E21D', SYSUTCDATETIME());
                """
            },
            Core = { Sql = ""   }, // To test skipping null or empty content
            Post = { Sql = null }, // To test skipping null or empty content
        };

        await _connection.OpenAsync(cancellation);

        var migrations = await _connection.GetAppliedMigrationsAsync(null, cancellation);

        migrations.ShouldBeEmpty();

        await _connection.InitializeMigrationSupportAsync(cancellation);
        await _connection.InitializeMigrationSupportAsync(cancellation); // Test idempotency

        migrations = await _connection.GetAppliedMigrationsAsync(null, cancellation);

        migrations.ShouldBeEmpty();

        await _connection.ExecuteMigrationContentAsync(migration, MigrationPhase.Pre,  cancellation);
        await _connection.ExecuteMigrationContentAsync(migration, MigrationPhase.Core, cancellation);
        await _connection.ExecuteMigrationContentAsync(migration, MigrationPhase.Post, cancellation);

        migrations = await _connection.GetAppliedMigrationsAsync(null, cancellation);

        migrations         .ShouldHaveSingleItem();
        migrations[0].Name .ShouldBe("Test1");
        migrations[0].Hash .ShouldBe("F3BBBD66A63D4BF1747940578EC3D0103530E21D");
        migrations[0].State.ShouldBe(MigrationState.AppliedPre);
    }
}
