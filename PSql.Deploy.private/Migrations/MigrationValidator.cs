// Copyright 2023 Subatomix Research Inc.
// SPDX-License-Identifier: ISC

namespace PSql.Deploy.Migrations;

internal readonly ref struct MigrationValidator
{
    public SqlContext     Target  { get; }
    public MigrationPhase Phase   { get; }
    public IConsole       Console { get; }

    public MigrationValidator(SqlContext target, MigrationPhase phase, IConsole console)
    {
        Target  = target;
        Phase   = phase;
        Console = console;
    }

    // TODO
    private string MinimumMigrationName { get; } = "";

    internal bool Validate(ReadOnlySpan<Migration> migrations, SqlContext target)
    {
        var valid  = true;
        var lookup = CreateLookup(migrations);

        foreach (var migration in migrations)
        {
            valid &= ValidateNotChanged(migration, target);
            valid &= ValidateDepends   (migration, lookup);

            if (migration.IsAppliedThrough(Phase))
                continue; // Migration will not be applied

            valid &= ValidateCanApplyThroughPhase(migration, target);
            valid &= ValidateHasSource           (migration, target);
        }

        return valid;
    }

    private Dictionary<string, Migration> CreateLookup(ReadOnlySpan<Migration> migrations)
    {
        var lookup = new Dictionary<string, Migration>(
            capacity: migrations.Length, StringComparer.OrdinalIgnoreCase
        );

        foreach (var migration in migrations)
            if (!migration.IsPseudo && migration.Name is { } name)
                lookup.Add(name, migration);

        return lookup;
    }

    private bool ValidateDepends(Migration migration, Dictionary<string, Migration> dictionary)
    {
        if (migration.IsPseudo)
            return true;

        if (migration.Depends is not { } dependNames)
            return true;

        var resolvedDepends = new List<Migration>(dependNames.Count);
        var valid           = true;

        static int Compare(string? lhs, string? rhs)
            => StringComparer.OrdinalIgnoreCase.Compare(lhs, rhs);

        foreach (var dependName in dependNames)
        {
            switch (Compare(dependName, migration.Name))
            {
                case < 0 when dictionary.TryGetValue(dependName, out var depend):
                    resolvedDepends.Add(depend);
                    break;

                case < 0 when Compare(dependName, MinimumMigrationName) < 0:
                    Console.WriteVerbose(string.Format(
                        "Ignoring migration '{0}' dependency on migration '{1}', " +
                        "which is older than the earliest migration on disk.",
                        /*{0}*/ migration.Name,
                        /*{1}*/ dependName
                    ));
                    break;

                case < 0:
                    valid = false;
                    Console.WriteWarning(string.Format(
                        "Migration '{0}' declares a dependency on migration '{1}', " +
                        "which was not found. "                                      +
                        "The dependency cannot be satisfied.",
                        /*{0}*/ migration.Name,
                        /*{1}*/ dependName
                    ));
                    break;

                case > 0:
                    valid = false;
                    Console.WriteWarning(string.Format(
                        "Migration '{0}' declares a dependency on migration '{1}', " +
                        "which must run later in the sequence. "                     +
                        "The dependency cannot be satisfied.",
                        /*{0}*/ migration.Name,
                        /*{1}*/ dependName
                    ));
                    break;

                default:
                    valid = false;
                    Console.WriteWarning(string.Format(
                        "Migration '{0}' declares a dependency on itself. " +
                        "The dependency cannot be satisfied.",
                        /*{0}*/ migration.Name
                    ));
                    break;
            }
        }

        migration.ResolvedDepends = resolvedDepends;
        return valid;
    }

    private bool ValidateNotChanged(Migration migration, SqlContext target)
    {
        // Valid regardless of hash difference if migration is not yet applied
        if (migration.State2 == MigrationState.NotApplied)
            return true;

        // Valid if hash has not changed
        if (!migration.HasChanged)
            return true;

        Console.WriteWarning(string.Format(
            "Migration '{0}' has been applied to database [{1}].[{2}] through " +
            "the {3} phase, but the migration's code in the source directory "  +
            "does not match the code previously used. To resolve, revert any "  +
            "accidental changes to this migration. To ignore, update the hash " +
            "in the _deploy.Migration table to match the hash of the code in "  +
            "the source directory ({4}).",
            /*{0}*/ migration.Name,
            /*{1}*/ target.GetEffectiveServerName(),
            /*{2}*/ target.DatabaseName,
            /*{3}*/ migration.AppliedThroughPhase,
            /*{4}*/ migration.Hash
        ));

        return false;
    }

    private bool ValidateCanApplyThroughPhase(Migration migration, SqlContext target)
    {
        if (migration.CanApplyThrough(Phase))
            return true;

        Console.WriteWarning(string.Format(
            "Cannot apply {3} phase of migration '{0}' to database [{1}].[{2}] " +
            "because the migration has code in an earlier phase that must be "   +
            "applied first.",
            /*{0}*/ migration.Name,
            /*{1}*/ target.GetEffectiveServerName(),
            /*{2}*/ target.DatabaseName,
            /*{3}*/ Phase
        ));

        return false;
    }

    private bool ValidateHasSource(Migration migration, SqlContext target)
    {
        // Valid if there is a path to the migration code
        if (migration.Path is not null)
            return true;

        Console.WriteWarning(string.Format(
            "Migration {0} is only partially applied to database [{1}].[{2}] " +
            "(through the {3} phase), but the code for the migration was not " +
            "found in the source directory. It is not possible to complete "   +
            "this migration.",
            /*{0}*/ migration.Name,
            /*{1}*/ target.GetEffectiveServerName(),
            /*{2}*/ target.DatabaseName,
            /*{3}*/ migration.AppliedThroughPhase
        ));

        return false;
    }
}
