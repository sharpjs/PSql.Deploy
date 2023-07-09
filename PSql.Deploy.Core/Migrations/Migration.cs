// Copyright 2023 Subatomix Research Inc.
// SPDX-License-Identifier: ISC

namespace PSql.Deploy.Migrations; 

/// <summary>
///   A database schema migration.
/// </summary>
public class Migration
{
    /// <summary>
    ///   The name of a pseudo-migration that runs before any others.
    /// </summary>
    public const string BeginPseudoMigrationName = "_Begin";

    /// <summary>
    ///   The name of a pseudo-migration that runs after any others.
    /// </summary>
    public const string EndPseudoMigrationName = "_End";

    /// <summary>
    ///   Initializes a new <see cref="Migration"/> instance with the specified
    ///   name.
    /// </summary>
    /// <exception cref="ArgumentNullException">
    ///   <paramref name="name"/> is <see langword="null"/>.
    /// </exception>
    /// <exception cref="ArgumentException">
    ///   <paramref name="name"/> is the empty string.
    /// </exception>
    public Migration(string name)
    {
        if (name is null)
            throw new ArgumentNullException(nameof(name));
        if (name.Length == 0)
            throw new ArgumentException("Argument cannot be empty.", nameof(name));

        Name     = name;
        IsPseudo = MigrationComparer.GetRank(name) != 0;
    }

    /// <summary>
    ///   Creates a new <see cref="Migration"/> instance that is a shallow
    ///   clone of the current instance.
    /// </summary>
    [Obsolete("To be removed when no longer needed.")]
    public Migration Clone()
    {
        return new(Name)
        {
            Path            = Path,
            Hash            = Hash,
            State           = State,
            HasChanged      = HasChanged,
            PreSql          = PreSql,
            CoreSql         = CoreSql,
            PostSql         = PostSql,
            Depends         = Depends,
            ResolvedDepends = ResolvedDepends,
        };
    }

    /// <summary>
    ///   Gets the name of the migration.
    /// </summary>
    public string Name { get; }

    /// <summary>
    ///   Gets whether the migration is a <c>_Begin</c> or <c>_End</c>
    ///   pseudo-migration.
    /// </summary>
    public bool IsPseudo { get; }

    /// <summary>
    ///   Gets or sets the full path <c>_Main.sql</c> file of the migration, or
    ///   <see langword="null"/> if no path is known.  The default value is
    ///   <see langword="null"/>.
    /// </summary>
    public string? Path { get; set; }

    /// <summary>
    ///   Gets or sets the hash computed from the SQL content of the migration.
    ///   The default value is the empty string.
    /// </summary>
    public string Hash { get; set; } = "";

    /// <summary>
    ///   Gets or sets the application state of the migration.  The default
    ///   value is <see cref="MigrationState.NotApplied"/>.
    /// </summary>
    public MigrationState State { get; set; }

    /// <summary>
    ///   Gets or sets whether the migration has changed after it was deployed.
    ///   The default value is <see langword="false"/>.
    /// </summary>
    public bool HasChanged { get; set; }

    /// <summary>
    ///   Gets or sets whether the migration's SQL scripts and dependency names
    ///   have been loaded.  The default value is <see langword="false"/>.
    /// </summary>
    internal bool IsContentLoaded { get; set; }

    /// <summary>
    ///   Gets or sets the SQL script for the <b>Pre</b> phase, or
    ///   <see langword="null"/> if no script is known.  The default value is
    ///   <see langword="null"/>.
    /// </summary>
    public string? PreSql { get; set; }

    /// <summary>
    ///   Gets or sets the SQL script for the <b>Core</b> phase, or
    ///   <see langword="null"/> if no script is known.  The default value is
    ///   <see langword="null"/>.
    /// </summary>
    public string? CoreSql { get; set; }

    /// <summary>
    ///   Gets or sets the SQL script for the <b>Post</b> phase, or
    ///   <see langword="null"/> if no script is known.  The default value is
    ///   <see langword="null"/>.
    /// </summary>
    public string? PostSql { get; set; }

    /// <summary>
    ///   Gets or sets the names of migrations that must be applied completely
    ///   before any phase of the current migration.  The default value is an
    ///   empty list.
    /// </summary>
    public IReadOnlyList<string> Depends { get; set; } = Array.Empty<string>();

    /// <summary>
    ///   Gets or sets the resolved migrations that must be applied completely
    ///   before any phase of the current migration.  The default value is an
    ///   empty list.
    /// </summary>
    internal IReadOnlyList<Migration> ResolvedDepends { get; set; } = Array.Empty<Migration>();

    /// <summary>
    ///   Gets or sets the diagnostic messages associated with the migration.
    ///   The default value is an empty list.
    /// </summary>
    internal IReadOnlyList<MigrationDiagnostic> Diagnostics { get; set; } = Array.Empty<MigrationDiagnostic>();

    /// <inheritdoc/>
    public override string ToString() => Name;

    /// <summary>
    ///   Gets the SQL script for the specified phase.
    /// </summary>
    /// <param name="phase">
    ///   The phase for which to get the SQL script.
    /// </param>
    /// <exception cref="ArgumentOutOfRangeException">
    ///   <paramref name="phase"/> is not a valid <see cref="MigrationPhase"/>.
    /// </exception>
    public string? GetSql(MigrationPhase phase)
    {
        return phase switch
        {
            MigrationPhase.Pre  => PreSql,
            MigrationPhase.Core => CoreSql,
            MigrationPhase.Post => PostSql,
            _ => throw new ArgumentOutOfRangeException(nameof(phase)),
        };
    }

    /// <summary>
    ///   Gets the latest phase of the migration that has been applied to a
    ///   target database, or <see langword="null"/> if the migration has not
    ///   been applied in any phase.
    /// </summary>
    public MigrationPhase? LatestAppliedPhase => State switch
    {
        MigrationState.NotApplied => null,
        var state                 => (MigrationPhase) (state - 1)
    };

    /// <summary>
    ///   Checks whether the migration has been applied through the specified
    ///   phase.
    /// </summary>
    /// <param name="phase">
    ///   The phase to check.
    /// </param>
    public bool IsAppliedThrough(MigrationPhase phase)
    {
        // State also functions as 'next phase to be applied'
        return (MigrationPhase) State > phase;
    }

    /// <summary>
    ///   Checks whether the migration can be applied through the specified
    ///   phase.
    /// </summary>
    /// <param name="phase">
    ///   The phase to check.
    /// </param>
    public bool CanApplyThrough(MigrationPhase phase)
    {
        // Pseudo-migrations always can be applied
        if (IsPseudo)
            return true;

        // State also functions as 'next phase to be applied'
        var next = (MigrationPhase) State;

        // If the next phase to be applied is later than the requested state,
        // the migration has already been applied and cannot be applied again
        if (next > phase)
            return false;

        // If the next phase to be applied is the same as the requested state,
        // the migration can be applied
        if (next == phase)
            return true;

        // If the next phase to be applied is earlier than the requested state,
        // the migration can be applied only if the intermediate phases are empty
        for (; next < phase; next++)
            if (!GetSql(next).IsNullOrEmpty())
                return false;

        // Intermediate phases are indeed empty
        return true;
    }
}
