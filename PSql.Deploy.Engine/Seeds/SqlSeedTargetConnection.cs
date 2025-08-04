// Copyright Subatomix Research Inc.
// SPDX-License-Identifier: MIT

namespace PSql.Deploy.Seeds;

/// <inheritdoc cref="ISeedTargetConnection"/>
internal class SqlSeedTargetConnection : SqlTargetConnection, ISeedTargetConnection
{
    /// <summary>
    ///   Initializes a new <see cref="SqlSeedTargetConnection"/> instance.
    /// </summary>
    /// <inheritdoc cref="SqlTargetConnection(Target, ISqlMessageLogger)"/>
    public SqlSeedTargetConnection(Target target, ISqlMessageLogger logger)
        : base(target, logger) { }

    /// <inheritdoc/>
    public async Task PrepareAsync(Guid runId, int workerId, CancellationToken cancellation = default)
    {
        var sql = GetSql("Prepare.sql");

        SetUpCommand(sql, timeout: 10, [
            ("RunId",    runId),
            ("WorkerId", workerId)
        ]);

        await Command.ExecuteNonQueryAsync(cancellation);

        ThrowIfHasErrors();
    }

    /// <inheritdoc/>
    public async Task ExecuteSeedBatchAsync(string sql, CancellationToken cancellation = default)
    {
        if (sql is null)
            throw new ArgumentNullException(nameof(sql));

        SetUpCommand(sql);

        await Command.ExecuteNonQueryAsync(cancellation);

        ThrowIfHasErrors();
    }

    private static string GetSql(string name)
    {
        return EmbeddedResource.Load(typeof(SqlSeedTargetConnection), name);
    }
}
