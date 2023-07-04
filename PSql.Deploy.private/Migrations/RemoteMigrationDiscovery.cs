// Copyright 2023 Subatomix Research Inc.
// SPDX-License-Identifier: ISC

namespace PSql.Deploy.Migrations;

using static CommandBehavior;

internal static class RemoteMigrationDiscovery
{
    internal static async Task<IReadOnlyList<Migration>> GetServerMigrationsAsync(
        SqlContext        context,
        string            minimumName,
        IConsole          console,
        CancellationToken cancellation)
    {
        if (context is null)
            throw new ArgumentNullException(nameof(context));
        if (minimumName is null)
            throw new ArgumentNullException(nameof(minimumName));
        if (console is null)
            throw new ArgumentNullException(nameof(console));

        await using var connection = context.Connect(null, console);
        await using var command    = connection.CreateCommand();

        command.CommandText =
            $"""
            IF OBJECT_ID('_deploy.Migration', 'U') IS NOT NULL
            EXEC sp_executesql
            N'
                SELECT Name, Hash, State
                FROM _deploy.Migration
                WHERE State < 3 OR Name >= @MinimumName
                ORDER BY Name
            ;',
            N'@MinimumName nvarchar(MAX)',
            N'{minimumName.Replace("'", "''")}';
            """;

        await using var reader = await command
            .GetRealSqlCommand()
            .ExecuteReaderAsync(SequentialAccess | SingleResult, cancellation);

        var migrations = new List<Migration>();

        while (await reader.ReadAsync(cancellation))
            migrations.Add(MapToMigration(reader));

        return migrations.AsReadOnly();
    }

    private static Migration MapToMigration(SqlDataReader reader)
    {
        return new Migration(reader.GetString(0))
        {
            Hash   = reader.GetString(1),
            State2 = (MigrationState) reader.GetInt32(2),
        };
    }
}
