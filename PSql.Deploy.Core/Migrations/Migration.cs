// Copyright 2023 Subatomix Research Inc.
// SPDX-License-Identifier: ISC

using System.Diagnostics;

namespace PSql.Deploy.Migrations;

/// <summary>
///   A database schema migration.
/// </summary>
[DebuggerDisplay(@"\{{Name}, {State}\}")]
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
    ///   Gets the migration content for the <b>Pre</b> phase.
    /// </summary>
    public MigrationContent Pre { get; } = new();

    /// <summary>
    ///   Gets the migration content for the <b>Core</b> phase.
    /// </summary>
    public MigrationContent Core { get; } = new();

    /// <summary>
    ///   Gets the migration content for the <b>Post</b> phase.
    /// </summary>
    public MigrationContent Post { get; } = new();

    /// <summary>
    ///   Gets or sets references to migrations that must be applied completely
    ///   before any phase of the current migration.  The default value is an
    ///   empty array.
    /// </summary>
    public ImmutableArray<MigrationReference> DependsOn { get; set; }
        = ImmutableArray<MigrationReference>.Empty;

    /// <summary>
    ///   Gets or sets the diagnostic messages associated with the migration.
    ///   The default value is an empty list.
    /// </summary>
    internal IReadOnlyList<MigrationDiagnostic> Diagnostics { get; set; }
        = Array.Empty<MigrationDiagnostic>();

    /// <summary>
    ///   Gets the content for the specified phase.
    /// </summary>
    /// <param name="phase">
    ///   The phase for which to get the content.
    /// </param>
    /// <returns>
    ///   The content for <paramref name="phase"/>.
    /// </returns>
    /// <exception cref="ArgumentOutOfRangeException">
    ///   <paramref name="phase"/> is not a valid <see cref="MigrationPhase"/>.
    /// </exception>
    public MigrationContent this[MigrationPhase phase]
    {
        get => phase switch
        {
            MigrationPhase.Pre  => Pre,
            MigrationPhase.Core => Core,
            MigrationPhase.Post => Post,
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

    /// <inheritdoc/>
    public override string ToString() => Name;
}
