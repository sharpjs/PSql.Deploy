// Copyright Subatomix Research Inc.
// SPDX-License-Identifier: MIT

namespace PSql.Deploy.Migrations;

[TestFixture]
public class SqlMigrationTargetConnectionTests
{
    [Test]
    public async Task ExecuteMigrationContentAsync_NullMigration()
    {
        await using var connection = new SqlMigrationTargetConnection(
            new("Server=."), new TestSqlLogger()
        );

        await Should.ThrowAsync<ArgumentNullException>(() =>
        {
            return connection.ExecuteMigrationContentAsync(null!, MigrationPhase.Core);
        });
    }
}
