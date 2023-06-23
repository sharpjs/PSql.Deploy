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
            State      = State,
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
    public ICollection<string>? Depends { get; set; }

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
}
