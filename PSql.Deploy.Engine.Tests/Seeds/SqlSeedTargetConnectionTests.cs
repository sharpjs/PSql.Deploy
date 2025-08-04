// Copyright Subatomix Research Inc.
// SPDX-License-Identifier: MIT

namespace PSql.Deploy.Seeds;

[TestFixture]
public class SqlSeedTargetConnectionTests
{
    [Test]
    public async Task ExecuteSeedBatchAsync_NullSql()
    {
        await using var connection = new SqlSeedTargetConnection(
            new("Server=."), new TestSqlLogger()
        );

        await Should.ThrowAsync<ArgumentNullException>(() =>
        {
            return connection.ExecuteSeedBatchAsync(null!);
        });
    }
}
