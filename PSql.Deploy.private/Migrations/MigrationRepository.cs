// Copyright Subatomix Research Inc.
// SPDX-License-Identifier: MIT

extern alias Engine;

using M = Engine::PSql.Deploy.Migrations;
using Inner = Engine::PSql.Deploy.Migrations.MigrationRepository;

namespace PSql.Deploy.Migrations;

internal static class MigrationRepository
{
    public static ImmutableArray<IMigration> GetAll(
        string  path,
        string? maxName = null)
    {
        return ImmutableArray.CreateRange(Inner.GetAll(path, maxName), Lift);
    }

    public static async Task<IReadOnlyList<IMigration>> GetAllAsync(
        Target            target,
        string            minimumName,
        ICmdlet           cmdlet,
        CancellationToken cancellation)
    {
        var ms = await Inner.GetAllAsync(
            target.InnerTarget,
            minimumName,
            new PSSqlMessageLogger(cmdlet),
            cancellation
        );

        return ms.Select(Lift).ToList().AsReadOnly(); // immutable list instead?
    }

    private static IMigration Lift(M.Migration migration)
        => new Migration(migration);
}
