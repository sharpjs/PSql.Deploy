// Copyright 2023 Subatomix Research Inc.
// SPDX-License-Identifier: ISC

namespace PSql.Deploy.Migrations;

using static CommandBehavior;

internal static class RemoteMigrationDiscovery
{
    internal static IReadOnlyList<Migration> GetServerMigrations(SqlContext context, Cmdlet cmdlet)
    {
        using var connection = context.Connect(null, cmdlet);
        using var command    = connection.CreateCommand();

        command.CommandText =
            """
            IF OBJECT_ID('_deploy.Migration', 'U') IS NOT NULL
                EXEC('SELECT Name, Hash, State FROM _deploy.Migration ORDER BY Name;');
            """;

        using var reader = command
            .GetRealSqlCommand()
            .ExecuteReader(SequentialAccess | SingleResult);

        var migrations = new List<Migration>();

        while (reader.Read())
            migrations.Add(MapToMigration(reader));

        return migrations.AsReadOnly();
    }

    private static Migration MapToMigration(SqlDataReader reader)
    {
        return new Migration
        {
            Name  = reader.GetString(0),
            Hash  = reader.GetString(1).NullIfSpace(),
            State = reader.GetInt32(2),
        };
    }
}
