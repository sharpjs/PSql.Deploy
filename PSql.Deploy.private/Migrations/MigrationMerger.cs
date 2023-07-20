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
    ///   Initializes a new <see cref="MigrationMerger"/> instance.
    /// </summary>
    public MigrationMerger()
        : this(MigrationInternals.Instance) { }

    // For testing
    internal MigrationMerger(IMigrationInternals internals)
    {
        Internals = internals;
    }

    internal IMigrationInternals Internals { get; }

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
        Internals.LoadContent(definedMigration);

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
        var hasChanged = !HashEquals(definedMigration.Hash, appliedMigration.Hash);

        // It might be tempting to exclude completed migrations from the
        // merged migration list.  Do not.  They need to be present in case
        // they are dependency targets.

        // TODO: To reduce bloat in logs, do not mention a completed migration
        // in logs unless the migration is a dependency target.

        // Copy definition-only properties to applied
        appliedMigration.Path       = definedMigration.Path;
        appliedMigration.Hash       = definedMigration.Hash;
        appliedMigration.HasChanged = hasChanged;

        // If migration might be applied, ensure its content is loaded
        if (!appliedMigration.IsAppliedThrough(MigrationPhase.Post))
        {
            Internals.LoadContent(definedMigration);

            appliedMigration.Pre .Sql        = definedMigration.Pre .Sql;
            appliedMigration.Core.Sql        = definedMigration.Core.Sql;
            appliedMigration.Post.Sql        = definedMigration.Post.Sql;
            appliedMigration.Pre .IsRequired = definedMigration.Pre .IsRequired;
            appliedMigration.Core.IsRequired = definedMigration.Core.IsRequired;
            appliedMigration.Post.IsRequired = definedMigration.Post.IsRequired;
            appliedMigration.DependsOn       = definedMigration.DependsOn;
            appliedMigration.IsContentLoaded = definedMigration.IsContentLoaded;
        }

        return appliedMigration;
    }

    private static bool HashEquals(string definedHash, string appliedHash)
    {
        return string.IsNullOrWhiteSpace(appliedHash)
            || appliedHash.Equals(definedHash, StringComparison.OrdinalIgnoreCase);
    }
}
