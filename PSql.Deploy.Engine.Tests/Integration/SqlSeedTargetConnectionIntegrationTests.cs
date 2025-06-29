// Copyright Subatomix Research Inc.
// SPDX-License-Identifier: MIT

using PSql.Deploy.Seeds;

namespace PSql.Deploy.Integration.Seeds;

[TestFixture]
public class SqlSeedTargetConnectionIntegrationTests
{
    [Test]
    public async Task Test()
    {
        var runId    = new Guid("b1e62401-19d1-4b51-9fc4-f7118c6fa632");
        var workerId = 42;

        await using var connection = CreateConnection();

        await connection.OpenAsync();

        await connection.PrepareAsync(runId, workerId);

        await connection.ExecuteSeedBatchAsync(
            """
            DECLARE @RunIdGuid  uniqueidentifier = 'b1e62401-19d1-4b51-9fc4-f7118c6fa632';
            DECLARE @RunIdBytes binary(16)       = @RunIdGuid;
            DECLARE @WorkerId   int              = 42;

            IF CONTEXT_INFO()               = @RunIdBytes PRINT N'√ CONTEXT_INFO' ELSE SELECT 1/0;
            IF SESSION_CONTEXT(N'RunId')    = @RunIdGuid  PRINT N'√ RunId'        ELSE SELECT 1/0;
            IF SESSION_CONTEXT(N'WorkerId') = @WorkerId   PRINT N'√ WorkerId'     ELSE SELECT 1/0;
            """
        );
    }

    private static SqlSeedTargetConnection CreateConnection()
    {
        return new(IntegrationTestsSetup.Target, new TestSqlLogger());
    }
}
