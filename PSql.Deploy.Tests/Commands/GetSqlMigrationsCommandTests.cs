// Copyright Subatomix Research Inc.
// SPDX-License-Identifier: MIT

using PSql.Deploy.Migrations;

namespace PSql.Deploy.Commands;

using static ScriptExecutor;

[TestFixture]
public class GetSqlMigrationsCommandTests
{
    // This test fixture tests only discovery of migrations defined on disk.
    // Discovery of migrations registered in a target database is tested in
    // InvokeSqlMigrationsCommandIntegrationTests.

    [Test]
    public void Invoke()
    {
        var (output, exception) = Execute(
            """
            Get-SqlMigrations -Path (Join-Path TestDbs A)
            """
        );

        exception.ShouldBeNull();

        output.Count.ShouldBe(5);

        var migrations = output
            .Select(o => o.ShouldNotBeNull())
            .Select(o => o.BaseObject.ShouldBeOfType<Migration>())
            .ToList();

        var path = Path.Combine(TestContext.CurrentContext.TestDirectory, "TestDbs", "A");

        // From Pre+Core run
        migrations[0].Name    .ShouldBe("_Begin");
        migrations[0].Hash    .ShouldBe("1FA5BB132E282E92E49B1F3C73E5CA91977D1B7A");
        migrations[0].Path    .ShouldBe(Path.Combine(path, "Migrations", "_Begin", "_Main.sql"));
        migrations[0].State   .ShouldBe(MigrationState.NotApplied);
        migrations[0].IsPseudo.ShouldBeTrue();

        migrations[1].Name    .ShouldBe("Migration0");
        migrations[1].Hash    .ShouldBe("D8462C316FD72659FB11FA7C9727D05707F8332B");
        migrations[1].Path    .ShouldBe(Path.Combine(path, "Migrations", "Migration0", "_Main.sql"));
        migrations[1].State   .ShouldBe(MigrationState.NotApplied);
        migrations[1].IsPseudo.ShouldBeFalse();

        migrations[2].Name    .ShouldBe("Migration1");
        migrations[2].Hash    .ShouldBe("2909F7C67C9B831FFCD4655F31683941F700A205");
        migrations[2].Path    .ShouldBe(Path.Combine(path, "Migrations", "Migration1", "_Main.sql"));
        migrations[2].State   .ShouldBe(MigrationState.NotApplied);
        migrations[2].IsPseudo.ShouldBeFalse();

        migrations[3].Name    .ShouldBe("Migration2");
        migrations[3].Hash    .ShouldBe("FB049E6EA9DC10019088850C94E9C5D2661A6DE7");
        migrations[3].Path    .ShouldBe(Path.Combine(path, "Migrations", "Migration2", "_Main.sql"));
        migrations[3].State   .ShouldBe(MigrationState.NotApplied);
        migrations[3].IsPseudo.ShouldBeFalse();

        migrations[4].Name    .ShouldBe("_End");
        migrations[4].Hash    .ShouldBe("48E2F54503C2A75955805121FD5C24E2DE586489");
        migrations[4].Path    .ShouldBe(Path.Combine(path, "Migrations", "_End", "_Main.sql"));
        migrations[4].State   .ShouldBe(MigrationState.NotApplied);
        migrations[4].IsPseudo.ShouldBeTrue();
    }
}
