// Copyright Subatomix Research Inc.
// SPDX-License-Identifier: MIT

namespace PSql.Deploy.Migrations;

public static partial class MigrationRepository
{
    /// <summary>
    ///   TODO
    /// </summary>
    /// <param name="target"></param>
    /// <param name="minimumName"></param>
    /// <param name="logger"></param>
    /// <param name="cancellation"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentNullException"></exception>
    public static async Task<IReadOnlyList<Migration>> GetAllAsync(
        Target            target,
        string            minimumName,
        ISqlMessageLogger logger,
        CancellationToken cancellation)
    {
        if (target is null)
            throw new ArgumentNullException(nameof(target));
        if (minimumName is null)
            throw new ArgumentNullException(nameof(minimumName));
        if (logger is null)
            throw new ArgumentNullException(nameof(logger));

        var sql = 
            $"""
            IF OBJECT_ID(N'_deploy.Migration', N'U') IS NOT NULL
            EXEC sp_executesql
            N'
                SELECT Name, Hash, State
                FROM _deploy.Migration
                WHERE State < 3 OR Name >= @MinimumName
                ORDER BY Name
            ;',
            N'@MinimumName nvarchar(max)',
            N'{minimumName.EscapeForSqlString()}';
            """;

        await using var connection = new SqlTargetConnection(target, logger); // TODO: Make testable

        var migrations = new List<Migration>(); // TODO: ImmutableArray?

        await connection.OpenAsync(cancellation);
        await connection.ExecuteAsync(sql, AddMigration, migrations, cancellation);

        return migrations;
    }

    private static void AddMigration(IDataRecord record, List<Migration> migrations)
    {
        migrations.Add(MapToMigration(record));
    }

    private static Migration MapToMigration(IDataRecord record)
    {
        return new Migration(record.GetString(0))
        {
            Hash  = record.GetString(1),
            State = (MigrationState) record.GetInt32(2),
        };
    }
}
