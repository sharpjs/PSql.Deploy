// Copyright 2023 Subatomix Research Inc.
// SPDX-License-Identifier: ISC

using PSql.Deploy.Migrations;

namespace PSql.Deploy.Commands;

[Cmdlet(VerbsCommon.Get, "SqlMigrations", DefaultParameterSetName = "Path")]
[OutputType(typeof(Migration))]
public class GetSqlMigrationsCommand : AsyncCmdlet
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

    protected override async Task ProcessRecordAsync(CancellationToken cancellation)
    {
        var migrations
            = Path   is { } path   ? GetMigrations(path)
            : Target is { } target ? await GetMigrationsAsync(target, cancellation)
            : throw new InvalidOperationException();

        if (IncludeContent.IsPresent)
            Parallel.ForEach(migrations, MigrationLoader.LoadContent);

        foreach (var migration in migrations)
            Console.WriteObject(migration);
    }

    private static IReadOnlyList<Migration> GetMigrations(string path)
    {
        return MigrationRepository.GetAll(path);
    }

    private Task<IReadOnlyList<Migration>> GetMigrationsAsync(
        SqlContext target, CancellationToken cancellation)
    {
        return MigrationRepository.GetAllAsync(
            target, minimumName: "", Console, cancellation
        );
    }
}
