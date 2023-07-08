// Copyright 2023 Subatomix Research Inc.
// SPDX-License-Identifier: ISC

namespace PSql.Deploy.Migrations;

/// <summary>
///   An algorithm that merges an ordered collection of
///     <b>defined</b> migrations (found on the filesystem)
///   with an ordered collection of
///     <b>applied</b> migrations (recorded in a target database),
///   producing an ordered collection of
///     <b>pending</b> migrations (needing validation and/or application).
/// </summary>
internal readonly ref struct MigrationMerger
{
    /// <summary>
    ///   Initializes a new <see cref="MigrationMerger"/> instance for the
    ///   specified phase.
    /// </summary>
    /// <param name="phase">
    ///   The phase in which migrations are being applied.
    /// </param>
    public MigrationMerger(MigrationPhase phase)
    {
        Phase = phase;
    }

    /// <summary>
    ///   Gets the phase in which migrations are being applied.
    /// </summary>
    public MigrationPhase Phase { get; }

    /// <summary>
    ///   Merges the specified ordered collection of
    ///     <b>defined</b> migrations (found on the filesystem)
    ///   with the specified ordered collection of
    ///     <b>applied</b> migrations (recorded in a target database),
    ///   producing an ordered collection of
    ///     <b>pending</b> migrations (needing validation and/or application).
    /// </summary>
    /// <param name="definedMigrations">
    ///   The ordered collection of defined migrations.
    /// </param>
    /// <param name="appliedMigrations">
    ///   The ordered collection of applied migrations.
    /// </param>
    /// <returns>
    ///   An ordered collection of pending migrations.
    /// </returns>
    public ImmutableArray<Migration> Merge(
        ReadOnlySpan       <Migration> definedMigrations,
        IReadOnlyCollection<Migration> appliedMigrations)
    {
        // Assume migrations already sorted using MigrationComparer

        var pendingMigrations = ImmutableArray.CreateBuilder<Migration>( 
            initialCapacity: Math.Max(definedMigrations.Length, appliedMigrations.Count)
        );

              var definedItems = definedMigrations.GetEnumerator();
        using var appliedItems = appliedMigrations.GetEnumerator();

        var hasDefined = definedItems.MoveNext();
        var hasApplied = appliedItems.MoveNext();
        var hasPending = false;

        while (hasDefined || hasApplied)
        {
            Migration? migration;

            // Decide which migration comes next: defined, applied, or both
            var comparison
                = !hasDefined ? +1 // use applied migration
                : !hasApplied ? -1 // use defined migration
                : MigrationComparer.Instance.Compare(definedItems.Current, appliedItems.Current);

            // Consume that/those migration(s), potentionally merging
            if (comparison < 0)
            {
                // defined
                migration  = OnDefinedWithoutApplied(definedItems.Current);
                hasDefined = definedItems.MoveNext();
            }
            else if (comparison > 0)
            {
                // applied
                migration  = OnAppliedWithoutDefined(appliedItems.Current);
                hasApplied = appliedItems.MoveNext();
            }
            else
            {
                // both
                migration  = OnMatchedDefinedAndApplied(definedItems.Current, appliedItems.Current);
                hasDefined = definedItems.MoveNext();
                hasApplied = appliedItems.MoveNext();
            }

            if (migration is not null)
            {
                // The _Begin and _End pseudo-migrations are pending only if
                // there is at least one other pending migration.
                hasPending |= !migration.IsPseudo;
                pendingMigrations.Add(migration);
            }
        }

        return hasPending
            ? pendingMigrations.Build()
            : ImmutableArray<Migration>.Empty;
    }

    private Migration? OnDefinedWithoutApplied(Migration definedMigration)
    {
        // Migration will be applied; ensure its content is loaded
        MigrationLoader.LoadContent(definedMigration);

        return definedMigration;
    }

    private Migration? OnAppliedWithoutDefined(Migration appliedMigration)
    {
        // Skip a completed migration whose definition has disappeared; the
        // user must be able to clean up old migration files they do not need
        if (appliedMigration.IsAppliedThrough(MigrationPhase.Post))
            return null; // completed; definition removed

        // An incomplete, definitionless migration cannot be applied but needs
        // to be present in the pending migration list so that validation can
        // warn about it
        return appliedMigration;
    }

    private Migration? OnMatchedDefinedAndApplied(
        Migration definedMigration,
        Migration appliedMigration)
    {
        // Detect a hash mismatch unless the database opts out of hash checks
        // for a migration by setting the migration's hash to space characters
        var hasChanged
            =  !string.IsNullOrWhiteSpace(appliedMigration.Hash)
            && !appliedMigration.Hash.Equals(definedMigration.Hash, StringComparison.OrdinalIgnoreCase);

        // Skip a completed migration whose definition is unchanged; these
        // would be more bloat than useful information in the logs
        if (!hasChanged && appliedMigration.IsAppliedThrough(MigrationPhase.Post))
            return null;

        // If migration will be applied, ensure its content is loaded
        if (!appliedMigration.IsAppliedThrough(Phase))
            MigrationLoader.LoadContent(definedMigration);

        // Copy definition-only properties to applied
        appliedMigration.Path       = definedMigration.Path;
        appliedMigration.Hash       = definedMigration.Hash;
        appliedMigration.Depends    = definedMigration.Depends;
        appliedMigration.PreSql     = definedMigration.PreSql;
        appliedMigration.CoreSql    = definedMigration.CoreSql;
        appliedMigration.PostSql    = definedMigration.PostSql;
        appliedMigration.HasChanged = hasChanged;

        return appliedMigration;
    }
}
