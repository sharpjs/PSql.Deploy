// Copyright 2023 Subatomix Research Inc.
// SPDX-License-Identifier: ISC

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
