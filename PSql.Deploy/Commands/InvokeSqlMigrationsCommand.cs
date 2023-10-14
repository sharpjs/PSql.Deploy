// Copyright 2023 Subatomix Research Inc.
// SPDX-License-Identifier: ISC

using PSql.Deploy.Migrations;
using Subatomix.PowerShell.TaskHost;

namespace PSql.Deploy.Commands;

using static MigrationPhase;

[Cmdlet(
    VerbsLifecycle.Invoke, "SqlMigrations",
    DefaultParameterSetName = ContextParameterSetName
)]
public class InvokeSqlMigrationsCommand : PerSqlContextCommand
{
    // -Path
    [Parameter()]
    [Alias("PSPath", "SourcePath")]
    [ValidateNotNullOrEmpty]
    public string? Path { get; set; }

    // -Phase
    [Parameter()]
    [ValidateSet(nameof(Pre), nameof(Core), nameof(Post))]
    public MigrationPhase[]? Phase { get; set; }

    // -MaximumMigrationName
    [Parameter()]
    [ValidateNotNullOrEmpty]
    public string? MaximumMigrationName { get; set; }

    // -AllowCorePhase
    [Parameter()]
    public SwitchParameter AllowCorePhase { get; set; }

    // -WhatIf
    [Parameter()]
    public SwitchParameter WhatIf { get; set; }

    private static MigrationPhase[] AllPhases
        => new[] { Pre, Core, Post };

    private IMigrationSessionControl? _session;

    protected override void BeginProcessing() { } // do not call base
    protected override void EndProcessing()   { } // do not call base

    protected override void ProcessRecord()
    {
        if (TaskHost.Current is null)
            ReinvokeWithTaskHost();
        else
            ProcessRecordCore();
    }

    private void ReinvokeWithTaskHost()
    {
        using var invocation = new Invocation();

        invocation
            .AddReinvocation(MyInvocation)
            .UseTaskHost(this, withElapsed: true)
            .Invoke();
    }

    private void ProcessRecordCore()
    {
        var path = SessionState.Path.CurrentFileSystemLocation.Path;
        _session = MigrationSessionFactory.Create(path, CancellationToken);

        _session.AllowCorePhase = AllowCorePhase;
        _session.IsWhatIfMode   = WhatIf;

        _session.DiscoverMigrations(Path ?? path, MaximumMigrationName);

        foreach (var phase in Phase ?? AllPhases)
        {
            _session.Phase = phase;

            using var scope = TaskScope.Begin(_session.Phase.ToString());

            base.BeginProcessing();
            base.ProcessRecord();
            base.EndProcessing();
        }
    }

    protected override Task ProcessWorkAsync(SqlContextWork work)
    {
        return _session!.ApplyAsync(work, this);
    }
}
