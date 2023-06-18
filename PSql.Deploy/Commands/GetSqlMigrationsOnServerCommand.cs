// Copyright 2023 Subatomix Research Inc.
// SPDX-License-Identifier: ISC

using PSql.Deploy.Migrations;

namespace PSql.Deploy;

[Cmdlet(VerbsCommon.Get, "SqlMigrationsOnServer")]
[OutputType(typeof(Migration))]
public class GetSqlMigrationsOnServerCommand : Cmdlet
{
    // -Target
    [Parameter(Mandatory = true, Position = 0, ValueFromPipeline = true)]
    [ValidateNotNullOrEmpty]
    public SqlContext[]? Target { get; set; }

    protected override void ProcessRecord()
    {
        if (Target is not { Length: > 0 } contexts)
            return;

        foreach (var context in contexts)
            foreach (var migration in RemoteMigrationDiscovery.GetServerMigrations(context, this))
                WriteObject(migration);
    }
}
