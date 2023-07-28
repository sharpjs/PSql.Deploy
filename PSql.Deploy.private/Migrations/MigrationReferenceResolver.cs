// Copyright 2023 Subatomix Research Inc.
// SPDX-License-Identifier: ISC

namespace PSql.Deploy.Migrations;

internal static class MigrationReferenceResolver
{
    internal static void Resolve(ReadOnlySpan<Migration> migrations)
    {
        if (migrations.IsEmpty)
            return;

        var referencedNames = CollectReferencedNames(migrations);
        if (referencedNames is null)
            return;

        var migrationsByName = new Dictionary<string, Migration>(
            capacity: referencedNames.Count,
            StringComparer.OrdinalIgnoreCase
        );

        // Assume migrations are ordered such that a reference target precedes
        // all valid references to that target

        foreach (var migration in migrations)
        {
            // Pseudo-migrations cannot reference or be referenced by others
            if (migration.IsPseudo)
                continue;

            // Resolve references
            foreach (var reference in migration.DependsOn)
                if (migrationsByName.TryGetValue(reference.Name, out var resolved))
                    reference.Migration = resolved;

            // Make migration resolvable by subsequent references
            if (referencedNames.Contains(migration.Name))
                migrationsByName.Add(migration.Name, migration);
        }
    }

    private static HashSet<string>? CollectReferencedNames(ReadOnlySpan<Migration> migrations)
    {
        var names = null as HashSet<string>;

        foreach (var migration in migrations)
        {
            // Pseudo-migrations cannot reference or be referenced by others
            if (migration.IsPseudo)
                continue;

            // Collect references
            foreach (var reference in migration.DependsOn)
            {
                names ??= new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                names.Add(reference.Name);
            }
        }

        return names;
    }
}
