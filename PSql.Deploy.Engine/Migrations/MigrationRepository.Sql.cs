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
        string?           minimumName,
        ISqlMessageLogger logger,
        CancellationToken cancellation)
    {
        await using var connection = new SqlMigrationTargetConnection(target, logger); // TODO: Make testable

        await connection.OpenAsync(cancellation);

        return await connection.GetAppliedMigrationsAsync(minimumName, cancellation);
    }
}
