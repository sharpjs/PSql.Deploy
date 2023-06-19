// Copyright 2023 Subatomix Research Inc.
// SPDX-License-Identifier: ISC

using PSql.Deploy.Migrations;

namespace PSql.Deploy;

[Cmdlet(VerbsCommon.Get, "SqlMigrations", DefaultParameterSetName = "Path")]
[OutputType(typeof(Migration))]
public class GetSqlMigrationsCommand : Cmdlet
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
        var migrations
            = Path   is { } path   ? LocalMigrationDiscovery .GetLocalMigrations (path)
            : Target is { } target ? RemoteMigrationDiscovery.GetServerMigrations(target, this)
            : throw new InvalidOperationException();

        foreach (var migration in migrations)
            WriteObject(migration);
    }
}
