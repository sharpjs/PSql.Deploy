// Copyright 2023 Subatomix Research Inc.
// SPDX-License-Identifier: ISC

namespace PSql.Deploy.Migrations;

/// <summary>
///   A workspace for validating migrations.
/// </summary>
internal ref struct MigrationValidator
{
    /// <summary>
    ///   Initializes a new <see cref="MigrationValidator"/> instance.
    /// </summary>
    /// <param name="context">
    ///   Contextual information for validation.
    /// </param>
    /// <exception cref="ArgumentNullException">
    ///   <paramref name="context"/> is <see langword="null"/>.
    /// </exception>
    public MigrationValidator(IMigrationValidationContext context)
    {
        if (context is null)
            throw new ArgumentNullException(nameof(context));

        Context      = context;
        _isValid     = true;
        _diagnostics = new List<MigrationDiagnostic>();
    }

    /// <param name="context">
    ///   Gets contextual information for validation.
    /// </param>
    public IMigrationValidationContext Context { get; }

    // Whether all migrations are valid
    private bool _isValid;

    // Accumulator for diagnostics for the current migration
    private readonly List<MigrationDiagnostic> _diagnostics;

    /// <summary>
    ///   Validates the specified set of migrations.
    /// </summary>
    /// <param name="migrations">
    ///   The migrations to validate.
    /// </param>
    /// <returns>
    ///   <see langword="true"/> if all <paramref name="migrations"/> are valid;
    ///   <see langword="false"/> otherwise.
    /// </returns>
    /// <remarks>
    ///   This method populates the <see cref="Migration.Diagnostics"/>
    ///   property of each migration in <paramref name="migrations"/>.
    /// </remarks>
    internal bool Validate(ReadOnlySpan<Migration> migrations)
    {
        var lookup = CreateLookup(migrations);

        foreach (var migration in migrations)
        {
            ValidateCore(migration, lookup);

            migration.Diagnostics = _diagnostics.ToArray();
            _diagnostics.Clear();
        }

        return _isValid;
    }

    private void ValidateCore(Migration migration, Dictionary<string, Migration> lookup)
    {
        if (migration.IsPseudo)
            return;

        ValidateNotChanged(migration);
        ValidateDepends   (migration, lookup);

        if (migration.IsAppliedThrough(Context.Phase))
            return; // Migration will not be applied

        ValidateCanApplyThroughPhase(migration);
        ValidateHasSource           (migration);
    }

    private static Dictionary<string, Migration> CreateLookup(ReadOnlySpan<Migration> migrations)
    {
        var lookup = new Dictionary<string, Migration>(
            capacity: migrations.Length, StringComparer.OrdinalIgnoreCase
        );

        foreach (var migration in migrations)
            if (!migration.IsPseudo)
                lookup.Add(migration.Name, migration);

        return lookup;
    }

    private void ValidateDepends(Migration migration, Dictionary<string, Migration> dictionary)
    {
        if (migration.Depends.Count == 0)
            migration.ResolvedDepends = Array.Empty<Migration>();
        else
            ValidateDependsCore(migration, dictionary);
    }

    private void ValidateDependsCore(Migration migration, Dictionary<string, Migration> dictionary)
    {
        var resolvedDepends = new List<Migration>(migration.Depends.Count);

        static int Compare(string lhs, string rhs)
            => StringComparer.OrdinalIgnoreCase.Compare(lhs, rhs);

        foreach (var dependName in migration.Depends)
        {
            switch (Compare(dependName, migration.Name))
            {
                case < 0 when dictionary.TryGetValue(dependName, out var depend):
                    resolvedDepends.Add(depend);
                    break;

                case < 0 when Compare(dependName, Context.EarliestDefinedMigrationName) < 0:
                    AddWarning(string.Format(
                        "Ignoring migration '{0}' dependency on migration '{1}', " +
                        "which is older than the earliest migration on disk.",
                        /*{0}*/ migration.Name,
                        /*{1}*/ dependName
                    ));
                    break;

                case < 0:
                    AddError(string.Format(
                        "Migration '{0}' declares a dependency on migration '{1}', " +
                        "which was not found. "                                      +
                        "The dependency cannot be satisfied.",
                        /*{0}*/ migration.Name,
                        /*{1}*/ dependName
                    ));
                    break;

                case > 0:
                    AddError(string.Format(
                        "Migration '{0}' declares a dependency on migration '{1}', " +
                        "which must run later in the sequence. "                     +
                        "The dependency cannot be satisfied.",
                        /*{0}*/ migration.Name,
                        /*{1}*/ dependName
                    ));
                    break;

                default:
                    AddError(string.Format(
                        "Migration '{0}' declares a dependency on itself. " +
                        "The dependency cannot be satisfied.",
                        /*{0}*/ migration.Name
                    ));
                    break;
            }
        }

        migration.ResolvedDepends = resolvedDepends;
    }

    private void ValidateNotChanged(Migration migration)
    {
        // Valid regardless of hash difference if migration is not yet applied
        if (migration.State == MigrationState.NotApplied)
            return;

        // Valid if hash has not changed
        if (!migration.HasChanged)
            return;

        AddError(string.Format(
            "Migration '{0}' has been applied to database [{1}].[{2}] through " +
            "the {3} phase, but the migration's code in the source directory "  +
            "does not match the code previously used. To resolve, revert any "  +
            "accidental changes to this migration. To ignore, update the hash " +
            "in the _deploy.Migration table to match the hash of the code in "  +
            "the source directory ({4}).",
            /*{0}*/ migration.Name,
            /*{1}*/ Context.ServerName,
            /*{2}*/ Context.DatabaseName,
            /*{3}*/ migration.LatestAppliedPhase,
            /*{4}*/ migration.Hash
        ));
    }

    private void ValidateCanApplyThroughPhase(Migration migration)
    {
        if (migration.CanApplyThrough(Context.Phase))
            return;

        AddError(string.Format(
            "Cannot apply {3} phase of migration '{0}' to database [{1}].[{2}] " +
            "because the migration has code in an earlier phase that must be "   +
            "applied first.",
            /*{0}*/ migration.Name,
            /*{1}*/ Context.ServerName,
            /*{2}*/ Context.DatabaseName,
            /*{3}*/ Context.Phase
        ));
    }

    private void ValidateHasSource(Migration migration)
    {
        if (migration.Path is not null)
            return;

        AddError(string.Format(
            "Migration {0} is only partially applied to database [{1}].[{2}] " +
            "(through the {3} phase), but the code for the migration was not " +
            "found in the source directory. It is not possible to complete "   +
            "this migration.",
            /*{0}*/ migration.Name,
            /*{1}*/ Context.ServerName,
            /*{2}*/ Context.DatabaseName,
            /*{3}*/ migration.LatestAppliedPhase
        ));
    }

    private void AddError(string message)
    {
        _isValid = false;
        _diagnostics.Add(new(isError: true, message));
    }

    private void AddWarning(string message)
    {
        _diagnostics.Add(new(isError: false, message));
    }
}
