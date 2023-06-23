// Copyright 2023 Subatomix Research Inc.
// SPDX-License-Identifier: ISC

using PSql.Deploy.Migrations;

namespace PSql.Deploy.Commands;

// TODO: Replace original Invoke-SqlMigrations with this, removing the '2' suffix
//                                           V
[Cmdlet(VerbsLifecycle.Invoke, "SqlMigrations2")]
public class InvokeSqlMigrationsCommand : Cmdlet, IMigrationLogger
{
    // -Path
    [Parameter(Mandatory = true, Position = 0)]
    [Alias("PSPath")]
    [ValidateNotNullOrEmpty]
    public string? Path { get; set; }

    // -Target
    [Parameter(Mandatory = true, Position = 1, ValueFromPipeline = true)]
    [ValidateNotNullOrEmpty]
    public SqlContextParallelSet[]? Target { get; set; }

    protected override void BeginProcessing()
    {
    }

    void IMigrationLogger.Log(MigrationMessage message)
    {
        WriteHost(message.ToString()!);
    }
}