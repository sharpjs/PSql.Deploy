// Copyright Subatomix Research Inc.
// SPDX-License-Identifier: MIT

namespace PSql.Deploy.Seeds;

[TestFixture]
public class SqlSeedTargetConnectionTests
{
    [Test]
    public async Task ExecuteSeedBatchAsync_NullSql()
    {
        await using var c = new SqlSeedTargetConnection(new("Server=."), new TestSqlLogger());

        await Should.ThrowAsync<ArgumentNullException>(() =>
        {
            return c.ExecuteSeedBatchAsync(null!);
        });
    }
}
