// Copyright 2023 Subatomix Research Inc.
// SPDX-License-Identifier: ISC

using PSql.Deploy.Migrations;

namespace PSql.Deploy.Commands;

// TODO: Replace original Invoke-SqlMigrations with this, removing the '2' suffix
//                                           V
[Cmdlet(VerbsLifecycle.Invoke, "SqlMigrations2")]
public class InvokeSqlMigrationsCommand : AsyncCmdlet
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

    // -Phase
    [Parameter()]
    [ValidateNotNullOrEmpty]
    public MigrationPhase? Phase { get; set; }

    protected override async Task ProcessRecordAsync(CancellationToken cancellation)
    {
        var path   = SessionState.Path.CurrentFileSystemLocation.ProviderPath;
        var engine = new MigrationEngine(Console, path, cancellation);

        engine.DiscoverMigrations(Path!);
        engine.Phase = Phase ?? MigrationPhase.Post;

        await engine.ApplyAsync(Target!);
    }
}
