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
    ///   Validates the specified migration plan.
    /// </summary>
    /// <param name="plan">
    ///   The migration plan to validate.
    /// </param>
    /// <returns>
    ///   <see langword="true"/> if <paramref name="plan"/> is valid;
    ///   <see langword="false"/> otherwise.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    ///   <paramref name="plan"/> is <see langword="null"/>.
    /// </exception>
    /// <remarks>
    ///   This method populates the <see cref="Migration.Diagnostics"/>
    ///   property of each migration in <paramref name="plan"/>.
    /// </remarks>
    internal bool Validate(MigrationPlan plan)
    {
        if (plan is null)
            throw new ArgumentNullException(nameof(plan));

        foreach (var migration in plan.PendingMigrations)
        {
            ValidateCore(migration);
            migration.Diagnostics = TakeDiagnostics();
        }

        return _isValid;
    }

    private void ValidateCore(Migration migration)
    {
        if (migration.IsPseudo)
            return;

        ValidateNotChanged   (migration);
        ValidateDepends      (migration);
        ValidateApplicability(migration, out var applicability);

        if (applicability > Applicability.None)
            ValidateHasSource(migration);
    }

    private void ValidateDepends(Migration migration)
    {
        static int Compare(string lhs, string rhs)
            => StringComparer.OrdinalIgnoreCase.Compare(lhs, rhs);

        foreach (var reference in migration.DependsOn)
        {
            // If the reference was resolved, it is valid
            if (reference.Migration is not null)
                continue;

            // Otherwise, decide how invalid it is and why
            switch (Compare(reference.Name, migration.Name))
            {
                // Too old to be found
                case < 0 when Compare(reference.Name, Context.EarliestDefinedMigrationName) < 0:
                    AddWarning(string.Format(
                        "Ignoring migration '{0}' dependency on migration '{1}', " +
                        "which is older than the earliest migration on disk.",
                        /*{0}*/ migration.Name,
                        /*{1}*/ reference.Name
                    ));
                    break;

                // Not found
                case < 0:
                    AddError(string.Format(
                        "Migration '{0}' declares a dependency on migration '{1}', " +
                        "which was not found. "                                      +
                        "The dependency cannot be satisfied.",
                        /*{0}*/ migration.Name,
                        /*{1}*/ reference.Name
                    ));
                    break;

                // Too new
                case > 0:
                    AddError(string.Format(
                        "Migration '{0}' declares a dependency on migration '{1}', " +
                        "which must run later in the sequence. "                     +
                        "The dependency cannot be satisfied.",
                        /*{0}*/ migration.Name,
                        /*{1}*/ reference.Name
                    ));
                    break;

                // Self-reference
                default:
                    AddError(string.Format(
                        "Migration '{0}' declares a dependency on itself. " +
                        "The dependency cannot be satisfied.",
                        /*{0}*/ migration.Name
                    ));
                    break;
            }
        }
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

    private void ValidateApplicability(Migration migration, out Applicability applicability)
    {
        applicability = Applicability.None;

        CheckApplicability(migration.Pre,  ref applicability);
        CheckApplicability(migration.Core, ref applicability);
        CheckApplicability(migration.Post, ref applicability);

        if (applicability != Applicability.Blocked)
            return;

        AddError(string.Format(
            "Cannot apply migration '{0}' to database [{1}].[{2}] in the {3} " +
            "phase because the migration has code that must be applied in an " +
            "earlier phase first.",
            /*{0}*/ migration.Name,
            /*{1}*/ Context.ServerName,
            /*{2}*/ Context.DatabaseName,
            /*{3}*/ Context.Phase
        ));
    }

    private enum Applicability
    {
        None,       // nothing to apply
        Allowed,    // application allowed
        Blocked,    // application blocked (by required content in earlier phase)
    }

    private void CheckApplicability(MigrationContent content, ref Applicability applicability)
    {
        // Unplanned content has no bearing on a migration's applicability
        if (content.PlannedPhase is not { } plannedPhase)
            return;

        static int Compare(MigrationPhase lhs, MigrationPhase rhs)
            => ((int) lhs).CompareTo(((int) rhs));

        switch (Compare(plannedPhase, Context.Phase))
        {
            case > 0:
                // Content planned for a future phase has no bearing on a
                // migration's applicability in the current phase
                break;

            case < 0:
                // Content planned for an earlier phase, if required, blocks a
                // migration's applicability in the current phase; otherwise, no bearing
                if (content.IsRequired)
                    applicability = Applicability.Blocked;
                break;

            default:
                // Content planned for the current phase makes a migration
                // applicable unless otherwise blocked
                if (applicability == Applicability.None)
                    applicability = Applicability.Allowed;
                break;
        }
    }

    private void ValidateHasSource(Migration migration)
    {
        if (migration.Path is not null)
            return;

        if (migration.LatestAppliedPhase is { } phase)
            AddError(string.Format(
                "Migration '{0}' is only partially applied to database [{1}].[{2}] " +
                "(through the {3} phase), but the code for the migration was not "   +
                "found in the source directory. It is not possible to complete "     +
                "this migration.",
                /*{0}*/ migration.Name,
                /*{1}*/ Context.ServerName,
                /*{2}*/ Context.DatabaseName,
                /*{3}*/ phase
            ));
        else
            AddError(string.Format(
                "Migration '{0}' is registered in database [{1}].[{2}] but is not " +
                "applied in any phase, and the code for the migration was not "     +
                "found in the source directory. It is not possible to complete "    +
                "this migration.",
                /*{0}*/ migration.Name,
                /*{1}*/ Context.ServerName,
                /*{2}*/ Context.DatabaseName
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

    private IReadOnlyList<MigrationDiagnostic> TakeDiagnostics()
    {
        var diagnotics = _diagnostics.ToArray();
        _diagnostics.Clear();
        return diagnotics;
    }
}
