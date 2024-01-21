// Copyright 2024 Subatomix Research Inc.
// SPDX-License-Identifier: ISC

using PSql.Deploy.Migrations;
using Subatomix.PowerShell.TaskHost;

namespace PSql.Deploy.Commands;

using static MigrationPhase;

/// <summary>
///   The <c>Invoke-SqlMigrations</c> cmdlet.
/// </summary>
/// <remarks>
///   Invokes database schema migrations against sets of target databases.
/// </remarks>
[Cmdlet(
    VerbsLifecycle.Invoke, "SqlMigrations",
    DefaultParameterSetName = ContextParameterSetName,
    SupportsShouldProcess   = true, // -Confirm and -WhatIf
    ConfirmImpact           = ConfirmImpact.High
)]
public class InvokeSqlMigrationsCommand : PerSqlContextCommand
{
    /// <summary>
    ///   <b>-Path:</b>
    ///   Path to a directory containing migrations.
    /// </summary>
    [Parameter()]
    [Alias("PSPath", "SourcePath")]
    [ValidateNotNullOrEmpty]
    public string? Path { get; set; }

    /// <summary>
    ///   <b>-Phase:</b>
    ///   Deployment phases in which to run migrations.
    /// </summary>
    [Parameter()]
    [ValidateSet(nameof(Pre), nameof(Core), nameof(Post))]
    public MigrationPhase[]? Phase { get; set; }

    /// <summary>
    ///   <b>-MaximumMigrationName:</b>
    ///   Latest (maximum) name of migrations to discover.
    /// </summary>
    [Parameter()]
    [ValidateNotNullOrEmpty]
    public string? MaximumMigrationName { get; set; }

    /// <summary>
    ///   <b>-AllowCorePhase:</b>
    ///   Allow a non-skippable <c>Core</c> phase.
    /// </summary>
    [Parameter()]
    public SwitchParameter AllowCorePhase { get; set; }

    private bool IsWhatIf
        => this.IsWhatIf();

    private string CurrentPath
        => this.GetCurrentPath();

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
        var path    = CurrentPath;
        var console = new MigrationConsole(this);

        _session = MigrationSessionFactory.Create(console, path, CancellationToken);

        _session.AllowCorePhase = AllowCorePhase;
        _session.IsWhatIfMode   = IsWhatIf;

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
