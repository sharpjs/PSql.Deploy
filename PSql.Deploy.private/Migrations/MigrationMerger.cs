// Copyright 2023 Subatomix Research Inc.
// SPDX-License-Identifier: ISC

using System.Collections.Immutable;

namespace PSql.Deploy.Migrations;

internal readonly ref struct MigrationMerger
{
    public MigrationMerger(MigrationPhase phase)
    {
        Phase = phase;
    }

    public MigrationPhase Phase { get; }

    public ImmutableArray<Migration> Merge(
        ReadOnlySpan       <Migration> sourceMigrations,
        IReadOnlyCollection<Migration> targetMigrations)
    {
        // Assume migrations already sorted using MigrationComparer

        var migrations = ImmutableArray.CreateBuilder<Migration>( 
            initialCapacity: Math.Max(sourceMigrations.Length, targetMigrations.Count)
        );

              var sourceItems = sourceMigrations.GetEnumerator();
        using var targetItems = targetMigrations.GetEnumerator();

        var hasSource = sourceItems.MoveNext();
        var hasTarget = targetItems.MoveNext();

        while (hasSource || hasTarget)
        {
            Migration? migration;

            // Decide which migration comes next: source, target, or both
            var comparison
                = !hasSource ? +1 // use target migration
                : !hasTarget ? -1 // use source migration
                : MigrationComparer.Instance.Compare(sourceItems.Current, targetItems.Current);

            // Consume that/those migration(s), potentionally merging
            if (comparison < 0)
            {
                // source
                migration = OnSourceWithoutTarget(sourceItems.Current);
                hasSource = sourceItems.MoveNext();
            }
            else if (comparison > 0)
            {
                // target
                migration = OnTargetWithoutSource(targetItems.Current);
                hasTarget = targetItems.MoveNext();
            }
            else
            {
                // both
                migration = OnMatchedSourceAndTarget(sourceItems.Current, targetItems.Current);
                hasSource = sourceItems.MoveNext();
                hasTarget = targetItems.MoveNext();
            }

            if (migration is not null)
                migrations.Add(migration);
        }

        return migrations.Build();
    }

    private Migration? OnSourceWithoutTarget(Migration source)
    {
        // Migration will be applied; ensure its content is loaded
        MigrationLoader.LoadContent(source);

        return source;
    }

    private Migration? OnTargetWithoutSource(Migration target)
    {
        if (target.IsAppliedThrough(MigrationPhase.Post))
            return null; // completed; source migration removed

        return target;
    }

    private Migration? OnMatchedSourceAndTarget(Migration source, Migration target)
    {
        // If migration will be applied, ensure its content is loaded
        if (!target.IsAppliedThrough(Phase))
            MigrationLoader.LoadContent(source);

        // Copy source-only properties to target
        target.Path       = source.Path;
        target.HasChanged = !string.IsNullOrWhiteSpace(target.Hash)
            && !target.Hash.Equals(source.Hash, StringComparison.OrdinalIgnoreCase);
        target.Hash       = source.Hash;
        target.Depends    = source.Depends;
        target.PreSql     = source.PreSql;
        target.CoreSql    = source.CoreSql;
        target.PostSql    = source.PostSql;

        if (target.IsAppliedThrough(MigrationPhase.Post) && !target.HasChanged)
            return null; // completed

        // log if ($Migration.State -lt 3 -or $Migration.HasChanged)
        return target;
    }
}
