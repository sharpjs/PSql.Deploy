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
    ///   Initializes a new <see cref="Migration"/> instance.
    /// </summary>
    public Migration() { }

    /// <summary>
    ///   Creates a new <see cref="Migration"/> instance that is a shallow
    ///   clone of the current instance.
    /// </summary>
    public Migration Clone()
    {
        return new()
        {
            Name       = Name,
            Path       = Path,
            Hash       = Hash,
            State2     = State2,
            Depends    = Depends,
            PreSql     = PreSql,
            CoreSql    = CoreSql,
            PostSql    = PostSql,
            IsPseudo   = IsPseudo,
            HasChanged = HasChanged,
        };
    }

    /// <summary>
    ///   Gets or sets the name of the migration.
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    ///   Gets or sets the full path <c>_Main.sql</c> file of the migration.
    /// </summary>
    public string? Path { get; set; }

    /// <summary>
    ///   Gets or sets the hash computed from the SQL files of the migration.
    /// </summary>
    public string? Hash { get; set; }

    /// <summary>
    ///   Gets or sets the deployment state of the migration.
    /// </summary>
    /// <remarks>
    ///   <para>
    ///     The following values are possible:
    ///   </para>
    ///   <list type="table">
    ///     <item>
    ///       <term><c>0</c></term>
    ///       <description>not deployed</description>
    ///     </item>
    ///     <item>
    ///       <term><c>1</c></term>
    ///       <description>deployed partially, through phase <c>Pre</c></description>
    ///     </item>
    ///     <item>
    ///       <term><c>2</c></term>
    ///       <description>deployed partially, through phase <c>Core</c></description>
    ///     </item>
    ///     <item>
    ///       <term><c>3</c></term>
    ///       <description>deployed completely, through phase <c>Post</c></description>
    ///     </item>
    ///   </list>
    /// </remarks>
    [Obsolete("Use State2, which will be renamed to State soon.", error: true)]
    public int State
    {
        // TODO: Replace this property with State2
        get => (int) State2;
        set => State2 = (MigrationState) value;
    }

    /// <summary>
    ///   Gets or sets the application state of the migration.
    /// </summary>
    public MigrationState State2 { get; set; }

    /// <summary>
    ///   Gets or sets the names of migrations that must be applied completely
    ///   before any phase of the current migration.
    /// </summary>
    public IReadOnlyList<string>? Depends { get; set; }

    /// <summary>
    ///   Gets or sets the resolved migrations that must be applied completely
    ///   before any phase of the current migration.
    /// </summary>
    internal IReadOnlyList<Migration>? ResolvedDepends { get; set; }

    /// <summary>
    ///   Gets or sets the SQL script for the <b>Pre</b> phase.
    /// </summary>
    public string? PreSql { get; set; }

    /// <summary>
    ///   Gets or sets the SQL script for the <b>Core</b> phase.
    /// </summary>
    public string? CoreSql { get; set; }

    /// <summary>
    ///   Gets or sets the SQL script for the <b>Post</b> phase.
    /// </summary>
    public string? PostSql { get; set; }

    /// <summary>
    ///   Gets or sets whether the migration is a <c>_Begin</c> or <c>_End</c>
    ///   pseudo-migration.
    /// </summary>
    public bool IsPseudo { get; set; }

    /// <summary>
    ///   Gets or sets whether the migration has changed after it was deployed.
    /// </summary>
    public bool HasChanged { get; set; }

    /// <inheritdoc/>
    public override string ToString() => Name ?? "(unnamed)";

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
    public MigrationPhase? AppliedThroughPhase => State2 switch
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
        return (MigrationPhase) State2 > phase;
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
        var next = (MigrationPhase) State2;

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
        for (; next < phase;  next++)
            if (!GetSql(next).IsNullOrEmpty())
                return false;

        // Intermediate phases are indeed empty
        return true;
    }
}
