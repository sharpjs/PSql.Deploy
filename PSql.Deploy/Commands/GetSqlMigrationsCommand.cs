// Copyright Subatomix Research Inc.
// SPDX-License-Identifier: MIT

using PSql.Deploy.Migrations;

namespace PSql.Deploy.Commands;

// Resolve ambiguity
using AllowNullAttribute = System.Management.Automation.AllowNullAttribute;

/// <summary>
///   The <c>Get-SqlMigrations</c> command.
/// </summary>
/// <remarks>
///   Lists database schema migrations.
/// </remarks>
[Cmdlet(VerbsCommon.Get, "SqlMigrations", DefaultParameterSetName = "Path")]
[OutputType(typeof(Migration))]
public sealed class GetSqlMigrationsCommand : AsyncPSCmdlet
{
    /// <summary>
    ///   <b>-Path:</b> Path to a directory containing database source code in
    ///   the layout expected by PSql.Deploy.  The default is the current
    ///   directory.
    /// </summary>
    [Parameter(
        ParameterSetName  = "Path",
        Mandatory         = false,
        Position          = 0,
        ValueFromPipeline = true
    )]
    [Alias("PSPath")]
    [ValidateNotNullOrEmpty]
    public string? Path { get; set; }

#if INCLUDE_CONTENT
    /// <summary>
    ///   <b>-IncludeContent:</b>
    /// </summary>
    [Parameter(ParameterSetName = "Path")]
    public SwitchParameter IncludeContent { get; set; }
#endif

    /// <summary>
    ///   <b>-Target:</b> Object specifying how to connect to the database.
    /// </summary>
    /// <remarks>
    ///   string | SqlContext | SqlTargetDatabase
    /// </remarks>
    [Parameter(
        ParameterSetName  = "Target",
        Mandatory         = true,
        Position          = 0,
        ValueFromPipeline = true
    )]
    [ValidateNotNullOrEmpty]
    public SqlTargetDatabase? Target { get; set; }

    /// <summary>
    ///   <b>-MinimumName:</b> Minimum name of migration to return, or
    ///   <see langword="null"/> to to return all migrations registered on the
    ///   target database.  The default is <see langword="null"/>.
    /// </summary>
    [Parameter(ParameterSetName  = "Target")]
    [AllowNull, AllowEmptyString]
    public string? MinimumName { get; set; }

    protected override void ProcessRecord()
    {
        InvokePendingMainThreadActions();
        Run(ProcessRecordAsync);
    }

    private async Task ProcessRecordAsync()
    {
        var migrations = ParameterSetName switch
        {
            "Path" => GetMigrations(Path),
            _      => await GetMigrationsAsync(Target!),
        };

#if INCLUDE_CONTENT
        if (IncludeContent.IsPresent)
            Parallel.ForEach(migrations, MigrationLoader.LoadContent);
#endif

        foreach (var migration in migrations)
            WriteObject(new Migration(migration));
    }

    private IReadOnlyList<M.Migration> GetMigrations(string? path)
    {
        path = this.GetFullPath(path);

        return M.MigrationDiscoverer.GetAll(path);
    }

    private async Task<IReadOnlyList<M.Migration>> GetMigrationsAsync(SqlTargetDatabase target)
    {
        using var session = new M.MigrationSession(
            new()
            {
                EnabledPhases = [M.MigrationPhase.Pre],
                IsWhatIfMode  = true,
            },
            M.NullMigrationConsole.Instance
        );

        return await session.GetRegisteredMigrationsAsync(
            target.InnerTarget,
            MinimumName,
            new CmdletSqlMessageLogger(this)
        );
    }
}
