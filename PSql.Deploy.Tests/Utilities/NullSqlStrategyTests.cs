// Copyright Subatomix Research Inc.
// SPDX-License-Identifier: MIT

using PSql.Internal;

namespace PSql.Deploy;

[TestFixture]
public class NullSqlStrategyTests
{
    [Test]
    public async Task ConnectAsync()
    {
        var connection = await NullSqlStrategy.Instance.ConnectAsync(null!, null!, default);

        connection.Should().BeOfType<NullSqlConnection>();
    }

    [Test]
    public async Task ExecuteNonQueryAsync()
    {
        await NullSqlStrategy.Instance.ExecuteNonQueryAsync(null!, default);
    }
}
