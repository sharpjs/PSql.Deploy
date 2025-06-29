// Copyright Subatomix Research Inc.
// SPDX-License-Identifier: MIT

using PSql.Deploy.Migrations;

namespace PSql.Deploy.Integration.Migrations;

[TestFixture]
public class SqlMigrationTargetConnectionIntegrationTests
{
    [Test]
    public async Task Test()
    {
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

        await using var connection = CreateConnection();

        await connection.OpenAsync();

        var migrations = await connection.GetAppliedMigrationsAsync(null);

        migrations.ShouldBeEmpty();

        await connection.InitializeMigrationSupportAsync();
        await connection.InitializeMigrationSupportAsync(); // Test idempotency

        migrations = await connection.GetAppliedMigrationsAsync(null);

        migrations.ShouldBeEmpty();

        await connection.ExecuteMigrationContentAsync(migration, MigrationPhase.Pre);
        await connection.ExecuteMigrationContentAsync(migration, MigrationPhase.Core);
        await connection.ExecuteMigrationContentAsync(migration, MigrationPhase.Post);

        migrations = await connection.GetAppliedMigrationsAsync(null);

        migrations         .ShouldHaveSingleItem();
        migrations[0].Name .ShouldBe("Test1");
        migrations[0].Hash .ShouldBe("F3BBBD66A63D4BF1747940578EC3D0103530E21D");
        migrations[0].State.ShouldBe(MigrationState.AppliedPre);
    }

    private static SqlMigrationTargetConnection CreateConnection()
    {
        return new(IntegrationTestsSetup.Target, new TestSqlLogger());
    }
}
