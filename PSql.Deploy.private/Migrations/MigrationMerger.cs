// Copyright 2023 Subatomix Research Inc.
// SPDX-License-Identifier: ISC

namespace PSql.Deploy.Migrations;

/// <summary>
///   An algorithm that merges an ordered collection of source migrations
///   (found on the filesystem) with an ordered collection of target migrations
///   (recorded in a target database).
/// </summary>
internal readonly ref struct MigrationMerger
{
    /// <summary>
    ///   Initializes a new <see cref="MigrationMerger"/> instance.
    /// </summary>
    /// <param name="phase">
    ///   The phase for which migrations are to be merged.
    /// </param>
    public MigrationMerger(MigrationPhase phase)
    {
        Phase = phase;
    }

    /// <summary>
    ///   Gets the phase for which migrations are to be merged.
    /// </summary>
    public MigrationPhase Phase { get; }

    /// <summary>
    ///   Merges an ordered collection of source migrations (found on the
    ///   filesystem) with an ordered collection of target migrations (recorded
    ///   in a target database).
    /// </summary>
    /// <param name="sourceMigrations">
    ///   The ordered collection of source migrations.
    /// </param>
    /// <param name="targetMigrations">
    ///   The ordered collection of target migrations.
    /// </param>
    /// <returns>
    ///   An ordered collection containing the union of
    ///   <paramref name="sourceMigrations"/> and
    ///   <paramref name="targetMigrations"/>.
    /// </returns>
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

    private Migration? OnSourceWithoutTarget(Migration sourceMigration)
    {
        // Migration will be applied; ensure its content is loaded
        MigrationLoader.LoadContent(sourceMigration);

        return sourceMigration;
    }

    private Migration? OnTargetWithoutSource(Migration targetMigration)
    {
        // Skip a completed migration whose source has disappeared; the user
        // must be able to clean up old migration files they do not need
        if (targetMigration.IsAppliedThrough(MigrationPhase.Post))
            return null; // completed; source migration removed

        // An incomplete, sourceless migration cannot be applied but needs to
        // be present in the merged migration list so that validation can warn
        // about it
        return targetMigration;
    }

    private Migration? OnMatchedSourceAndTarget(Migration sourceMigration, Migration targetMigration)
    {
        // Detect a hash mismatch unless the database opts out of hash checks
        // for a migration by setting the migration's hash to space characters
        var hasChanged
            =  !string.IsNullOrWhiteSpace(targetMigration.Hash)
            && !targetMigration.Hash.Equals(sourceMigration.Hash, StringComparison.OrdinalIgnoreCase);

        // Skip a completed migration whose source is unchanged; these would be
        // more bloat than useful information in the logs
        if (!hasChanged && targetMigration.IsAppliedThrough(MigrationPhase.Post))
            return null;

        // If migration will be applied, ensure its content is loaded
        if (!targetMigration.IsAppliedThrough(Phase))
            MigrationLoader.LoadContent(sourceMigration);

        // Copy source-only properties to target
        targetMigration.Path       = sourceMigration.Path;
        targetMigration.Hash       = sourceMigration.Hash;
        targetMigration.Depends    = sourceMigration.Depends;
        targetMigration.PreSql     = sourceMigration.PreSql;
        targetMigration.CoreSql    = sourceMigration.CoreSql;
        targetMigration.PostSql    = sourceMigration.PostSql;
        targetMigration.HasChanged = hasChanged;

        return targetMigration;
    }
}
