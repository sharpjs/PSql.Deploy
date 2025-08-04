// Copyright Subatomix Research Inc.
// SPDX-License-Identifier: MIT

namespace PSql.Deploy.Migrations;

using static CommandBehavior;

/// <inheritdoc cref="IMigrationTargetConnection"/>
internal class SqlMigrationTargetConnection : SqlTargetConnection, IMigrationTargetConnection
{
    // Cached SQL
    private object? _initializeMigrationSupportSql;
    private object? _getRegisteredMigrationsSql;

    /// <summary>
    ///   Initializes a new <see cref="SqlMigrationTargetConnection"/> instance.
    /// </summary>
    /// <inheritdoc cref="SqlTargetConnection(Target, ISqlMessageLogger)"/>
    public SqlMigrationTargetConnection(Target target, ISqlMessageLogger logger)
        : base(target, logger) { }

    private string InitializeMigrationSupportSql
        => GetSql(ref _initializeMigrationSupportSql, "InitializeMigrationSupport.sql");

    private string GetRegisteredMigrationsSql
        => GetSql(ref _getRegisteredMigrationsSql, "GetRegisteredMigrations.sql");

    /// <inheritdoc/>
    public async Task InitializeMigrationSupportAsync(CancellationToken cancellation = default)
    {
        SetUpCommand(InitializeMigrationSupportSql);

        await Command.ExecuteNonQueryAsync(cancellation);

        ThrowIfHasErrors();
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<Migration>> GetAppliedMigrationsAsync(
        string?           minimumName,
        CancellationToken cancellation = default)
    {
        SetUpCommand(GetRegisteredMigrationsSql, timeout: 30, ("MinimumName", minimumName));

        await using var reader = await Command.ExecuteReaderAsync(
            SequentialAccess | SingleResult, cancellation
        );

        var migrations = new List<Migration>();

        while (await reader.ReadAsync(cancellation))
            migrations.Add(MapToMigration(reader));

        ThrowIfHasErrors();
        return migrations;
    }

    /// <inheritdoc/>
    public async Task ExecuteMigrationContentAsync(
        Migration         migration,
        MigrationPhase    phase,
        CancellationToken cancellation = default)
    {
        if (migration is null)
            throw new ArgumentNullException(nameof(migration));

        if (migration[phase].Sql is not { Length: > 0 } sql)
            return;

        SetUpCommand(sql);

        await Command.ExecuteNonQueryAsync(cancellation);

        ThrowIfHasErrors();
    }

    private static Migration MapToMigration(SqlDataReader reader)
    {
        return new Migration(reader.GetString(0))
        {
            Hash = reader.GetString(1),
            State = (MigrationState) reader.GetInt32(2),
        };
    }

    private static string GetSql(ref object? location, string name)
    {
        return EmbeddedResource.LazyLoad(
            ref location, typeof(SqlMigrationTargetConnection), name
        );
    }
}
