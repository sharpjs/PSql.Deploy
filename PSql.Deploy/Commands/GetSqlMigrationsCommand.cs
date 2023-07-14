// Copyright 2023 Subatomix Research Inc.
// SPDX-License-Identifier: ISC

using PSql.Deploy.Migrations;
using PSql.Deploy.Utilities;

namespace PSql.Deploy.Commands;

[Cmdlet(VerbsCommon.Get, "SqlMigrations", DefaultParameterSetName = "Path")]
[OutputType(typeof(Migration))]
public class GetSqlMigrationsCommand : Cmdlet, IAsyncCmdlet
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
        using var scope = new AsyncCmdletScope(this);

        scope.Run(ProcessAsync);
    }

    private async Task ProcessAsync(IAsyncCmdletContext context)
    {
        var migrations
            = Path   is { } path   ? GetMigrations(path)
            : Target is { } target ? await GetMigrationsAsync(target, context)
            : throw new InvalidOperationException();

        if (IncludeContent.IsPresent)
            Parallel.ForEach(migrations, MigrationLoader.LoadContent);

        foreach (var migration in migrations)
            WriteObject(migration);
    }

    private static IReadOnlyList<Migration> GetMigrations(string path)
    {
        return MigrationRepository.GetAll(path);
    }

    private Task<IReadOnlyList<Migration>> GetMigrationsAsync(
        SqlContext target, IAsyncCmdletContext context)
    {
        return MigrationRepository.GetAllAsync(
            target, minimumName: "", console: this, context.CancellationToken
        );
    }
}
