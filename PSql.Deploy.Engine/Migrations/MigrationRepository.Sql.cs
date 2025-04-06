// Copyright Subatomix Research Inc.
// SPDX-License-Identifier: MIT

namespace PSql.Deploy.Migrations;

using static CommandBehavior;

internal static partial class MigrationRepository
{
    internal static async Task<IReadOnlyList<Migration>> GetAllAsync(
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

        await using var scope   = target.CreateConnectionScope(logger);
        await using var command = scope.Connection.CreateCommand();

        command.Connection         = scope.Connection;
        command.RetryLogicProvider = scope.Connection.RetryLogicProvider;
        command.CommandType        = CommandType.Text;
        command.CommandText        =
            $"""
            IF OBJECT_ID(N'_deploy.Migration', N'U') IS NOT NULL
            EXEC sp_executesql
            N'
                SELECT Name, Hash, State
                FROM _deploy.Migration
                WHERE State < 3 OR Name >= @MinimumName
                ORDER BY Name
            ;',
            N'@MinimumName nvarchar(MAX)',
            N'{minimumName.EscapeForSqlString()}';
            """;

        await scope.Connection.OpenAsync(cancellation);

        await using var reader = await command
            .ExecuteReaderAsync(SequentialAccess | SingleResult, cancellation);

        ImmutableArray.CreateBuilder<Migration>().MoveToImmutable();

        var migrations = new List<Migration>();

        while (await reader.ReadAsync(cancellation))
            migrations.Add(MapToMigration(reader));

        return migrations;
    }

    private static Migration MapToMigration(SqlDataReader reader)
    {
        return new Migration(reader.GetString(0))
        {
            Hash  = reader.GetString(1),
            State = (MigrationState) reader.GetInt32(2),
        };
    }
}
