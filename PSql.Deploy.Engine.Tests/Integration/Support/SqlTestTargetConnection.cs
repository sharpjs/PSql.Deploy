// Copyright Subatomix Research Inc.
// SPDX-License-Identifier: MIT

namespace PSql.Deploy.Integration;

internal class SqlTestTargetConnection : SqlTargetConnection
{
    public SqlTestTargetConnection(Target target)
        : base(target, TestSqlLogger.Instance) { }

    public async Task CreateDatabaseAsync(string name, CancellationToken cancellationToken = default)
    {
        var nameInQuote = name.Replace("]", "]]");

        SetUpCommand(
            $"""
            CREATE DATABASE [{name}] COLLATE Latin1_General_100_CI_AI_SC_UTF8;
            """,
            timeout: 10
        );

        await Command.ExecuteNonQueryAsync(cancellationToken);

        ThrowIfHasErrors();
    }

    public async Task RemoveDatabaseAsync(string name, CancellationToken cancellationToken = default)
    {
        var nameInString        = name        .Replace("'", "''");
        var nameInQuoteInString = nameInString.Replace("]", "]]");

        SetUpCommand(
            $"""
            IF DB_ID('{nameInString}') IS NOT NULL EXEC(N'
                ALTER DATABASE [{nameInQuoteInString}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
                DROP  DATABASE [{nameInQuoteInString}];
            ');
            """,
            timeout: 30
        );

        await Command.ExecuteNonQueryAsync(cancellationToken);

        ThrowIfHasErrors();
    }
}
