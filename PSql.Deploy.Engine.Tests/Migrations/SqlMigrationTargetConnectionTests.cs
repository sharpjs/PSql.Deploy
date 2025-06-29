// Copyright Subatomix Research Inc.
// SPDX-License-Identifier: MIT

namespace PSql.Deploy.Migrations;

[TestFixture]
public class SqlMigrationTargetConnectionTests
{
    [Test]
    public async Task ExecuteMigrationContentAsync_NullMigration()
    {
        await using var c = new SqlMigrationTargetConnection(new("Server=."), new TestSqlLogger());

        await Should.ThrowAsync<ArgumentNullException>(() =>
        {
            return c.ExecuteMigrationContentAsync(null!, MigrationPhase.Core);
        });
    }
}
