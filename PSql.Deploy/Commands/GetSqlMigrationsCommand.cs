// Copyright 2023 Subatomix Research Inc.
// SPDX-License-Identifier: ISC

using PSql.Deploy.Migrations;

namespace PSql.Deploy.Commands;

[Cmdlet(VerbsCommon.Get, "SqlMigrations", DefaultParameterSetName = "Path")]
[OutputType(typeof(Migration))]
public sealed class GetSqlMigrationsCommand : AsyncPSCmdlet
{
    // -Path
    [Parameter(
        ParameterSetName = "Path",  ValueFromPipeline               = true,
        Mandatory        = true,    ValueFromPipelineByPropertyName = true,
        Position         = 0
    )]
    [Alias("PSPath")]
    [ValidateNotNullOrEmpty]
    public string? Path { get; set; }

    // -IncludeContent
    [Parameter(ParameterSetName = "Path")]
    public SwitchParameter IncludeContent { get; set; }

    // -Target
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
