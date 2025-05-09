// Copyright Subatomix Research Inc.
// SPDX-License-Identifier: MIT

using PSql.Deploy.Migrations;

namespace PSql.Deploy.Commands;

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
    ///   <b>-Path:</b> TODO
    /// </summary>
    [Parameter(
        ParameterSetName = "Path",  ValueFromPipeline               = true,
        Mandatory        = true,    ValueFromPipelineByPropertyName = true,
        Position         = 0
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
    ///   <b>-Target:</b> TODO
    /// </summary>
    /// <remarks>
    ///   Target | SqlContext | string
    /// </remarks>
    [Parameter(
        ParameterSetName = "Target", ValueFromPipeline               = true,
        Mandatory        = true,     ValueFromPipelineByPropertyName = true,
        Position         = 0
    )]
    [ValidateNotNullOrEmpty]
    public SqlTargetDatabase? Target { get; set; }

    protected override void ProcessRecord()
    {
        InvokePendingMainThreadActions();
        Run(ProcessRecordAsync);
    }

    private async Task ProcessRecordAsync()
    {
        var migrations
            = Path   is { } path   ?       GetMigrations     (path)
            : Target is { } target ? await GetMigrationsAsync(target)
            : throw new InvalidOperationException(
                "Either the Path or the Target parameter must be given."
            );

#if INCLUDE_CONTENT
        if (IncludeContent.IsPresent)
            Parallel.ForEach(migrations, MigrationLoader.LoadContent);
#endif

        foreach (var migration in migrations)
            WriteObject(new Migration(migration));
    }

    private static IReadOnlyList<M.Migration> GetMigrations(string path)
    {
        return M.MigrationRepository.GetAll(path);
    }

    private Task<IReadOnlyList<M.Migration>> GetMigrationsAsync(SqlTargetDatabase target)
    {
        return M.MigrationRepository.GetAllAsync(
            target.InnerTarget,
            minimumName: "",
            new CmdletSqlMessageLogger(this),
            CancellationToken
        );
    }
}
