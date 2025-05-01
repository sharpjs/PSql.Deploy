// Copyright Subatomix Research Inc.
// SPDX-License-Identifier: MIT

namespace PSql.Deploy.Migrations;

using Inner = M.MigrationRepository;

internal static class MigrationRepository
{
    public static ImmutableArray<Migration> GetAll(
        string  path,
        string? maxName = null)
    {
        return ImmutableArray.CreateRange(Inner.GetAll(path, maxName), Lift);
    }

    public static async Task<IReadOnlyList<Migration>> GetAllAsync(
        Target            target,
        string            minimumName,
        ICmdlet           cmdlet,
        CancellationToken cancellation)
    {
        var ms = await Inner.GetAllAsync(
            target.InnerTarget,
            minimumName,
            new CmdletSqlMessageLogger(cmdlet),
            cancellation
        );

        return ms.Select(Lift).ToList().AsReadOnly(); // immutable list instead?
    }

    private static Migration Lift(M.Migration migration)
        => new(migration);
}
