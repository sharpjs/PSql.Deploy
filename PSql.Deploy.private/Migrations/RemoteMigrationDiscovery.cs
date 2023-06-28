// Copyright 2023 Subatomix Research Inc.
// SPDX-License-Identifier: ISC

namespace PSql.Deploy.Migrations;

using static CommandBehavior;

internal static class RemoteMigrationDiscovery
{
    internal static async Task<IReadOnlyList<Migration>> GetServerMigrationsAsync(
        SqlContext context, IConsole console, CancellationToken cancellation)
    {
        await using var connection = context.Connect(null, console);
        await using var command    = connection.CreateCommand();

        command.CommandText =
            """
            IF OBJECT_ID('_deploy.Migration', 'U') IS NOT NULL
                EXEC('SELECT Name, Hash, State FROM _deploy.Migration ORDER BY Name;');
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
        return new Migration
        {
            Name   = reader.GetString(0),
            Hash   = reader.GetString(1).NullIfSpace(),
            State2 = (MigrationState) reader.GetInt32(2),
        };
    }
}
