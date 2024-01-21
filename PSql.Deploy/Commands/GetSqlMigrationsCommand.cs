// Copyright 2024 Subatomix Research Inc.
// SPDX-License-Identifier: ISC

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

    /// <summary>
    ///   <b>-IncludeContent:</b> TODO
    /// </summary>
    [Parameter(ParameterSetName = "Path")]
    public SwitchParameter IncludeContent { get; set; }

    /// <summary>
    ///   <b>-Target:</b> TODO
    /// </summary>
    [Parameter(
        ParameterSetName = "Target", ValueFromPipeline               = true,
        Mandatory        = true,     ValueFromPipelineByPropertyName = true,
        Position         = 0
    )]
    [ValidateNotNullOrEmpty]
    public SqlContext? Target { get; set; }

    protected override void ProcessRecord()
    {
        Run(ProcessRecordAsync);
    }

    private async Task ProcessRecordAsync()
    {
        var migrations
            = Path   is { } path   ? GetMigrations(path)
            : Target is { } target ? await GetMigrationsAsync(target)
            : throw new InvalidOperationException(
                "Either the Path or the Target parameter must be given."
            );

        if (IncludeContent.IsPresent)
            Parallel.ForEach(migrations, MigrationLoader.LoadContent);

        foreach (var migration in migrations)
            WriteObject(migration);
    }

    private static IReadOnlyList<Migration> GetMigrations(string path)
    {
        return MigrationRepository.GetAll(path);
    }

    private Task<IReadOnlyList<Migration>> GetMigrationsAsync(SqlContext target)
    {
        return MigrationRepository.GetAllAsync(
            target,
            minimumName: "",
            new CmdletSqlMessageLogger(this),
            CancellationToken
        );
    }
}
