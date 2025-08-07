// Copyright Subatomix Research Inc.
// SPDX-License-Identifier: MIT

using System.Diagnostics;
using PSql.Deploy.Migrations;

namespace PSql.Deploy.Commands;

using static MigrationPhase;

/// <summary>
///   The <c>Invoke-SqlMigrations</c> cmdlet.
/// </summary>
/// <remarks>
///   Invokes database schema migrations against sets of target databases.
/// </remarks>
[Cmdlet(VerbsLifecycle.Invoke, "SqlMigrations",
    SupportsShouldProcess = true, // -Confirm and -WhatIf
    ConfirmImpact         = ConfirmImpact.High
)]
public class InvokeSqlMigrationsCommand : AsyncPSCmdlet
{
    /// <summary>
    ///   <b>-Target:</b>
    ///   Objects specifying target databases and parallelism for deployment.
    /// </summary>
    [Parameter(Position = 0, Mandatory = true, ValueFromPipeline = true)]
    [ValidateNotNullOrEmpty]
    public SqlTargetDatabaseGroup[]? Target { get; set; }

    /// <summary>
    ///   <b>-Path:</b>
    ///   Path to a directory containing migrations.
    /// </summary>
    [Parameter]
    [Alias("PSPath", "SourcePath")]
    [ValidateNotNullOrEmpty]
    public string? Path { get; set; }

    /// <summary>
    ///   <b>-Phase:</b>
    ///   Deployment phases for which to run migrations.
    /// </summary>
    [Parameter]
    [ValidateSet(nameof(Pre), nameof(Core), nameof(Post))]
    public MigrationPhase[]? Phase { get; set; }

    /// <summary>
    ///   <b>-MaximumMigrationName:</b>
    ///   Maximum name of migrations to invoke.
    /// </summary>
    [Parameter]
    [ValidateNotNullOrEmpty]
    public string? MaximumMigrationName { get; set; }

    /// <summary>
    ///   <b>-AllowCorePhase:</b>
    ///   Allow migration content in the <c>Core</c> phase.
    /// </summary>
    [Parameter]
    public SwitchParameter AllowContentInCorePhase { get; set; }

    /// <summary>
    ///   <b>-MaxErrorCount:</b>
    ///   Maximum count of errors to allow.  If the count of errors exceeds
    ///   this value, the command attempts to cancel in-progress operations and
    ///   terminates early.
    /// </summary>
    [Parameter]
    [ValidateRange(0, int.MaxValue)]
    public int MaxErrorCount { get; set; }

    private M.MigrationSession? _session;

    /// <inheritdoc/>
    protected override void BeginProcessing()
    {
        base.BeginProcessing();

        _session = new(
            GetOptions(),
            new CmdletMigrationConsole(this, this.GetCurrentPath())
        );

        _session.DiscoverMigrations(this.GetFullPath(Path), MaximumMigrationName);
    }

    /// <inheritdoc/>
    protected override void ProcessRecord()
    {
        AssumeBeginProcessingInvoked();

        if (Target is not null)
            foreach (var group in Target)
                if (ShouldProcess(group))
                    _session.BeginApplying(group.InnerGroup);
    }

    /// <inheritdoc/>
    protected override void EndProcessing()
    {
        AssumeBeginProcessingInvoked();

        Run(() => _session.CompleteApplyingAsync(CancellationToken));

        base.EndProcessing();
    }

    /// <inheritdoc/>
    protected override void Dispose(bool managed)
    {
        if (managed)
        {
            _session?.Dispose();
            _session = null;
        }

        base.Dispose(managed);
    }

    private M.MigrationSessionOptions GetOptions()
    {
        var options = new M.MigrationSessionOptions();

        if (Phase is not null)
            options.EnabledPhases = Phase.Select(p => (M.MigrationPhase) p);

        options.AllowContentInCorePhase = AllowContentInCorePhase;
        options.MaxErrorCount           = MaxErrorCount;
        options.IsWhatIfMode            = this.IsWhatIf();

        return options;
    }

    private bool ShouldProcess(SqlTargetDatabaseGroup group)
    {
        var action   = $"Applying migrations to {group}.";
        var question = $"Apply migrations to {group}?";

        return ShouldProcess(action, question, null, out var whyNot)
            || whyNot is ShouldProcessReason.WhatIf;
    }

    [Conditional("DEBUG")]
    [MemberNotNull(nameof(_session))]
#if !DEBUG
    [ExcludeFromCodeCoverage(Justification = "Uninvokable due to ConditionalAttribute.")]
#endif
    internal void AssumeBeginProcessingInvoked()
    {
        if (_session is null)
            throw new InvalidOperationException("BeginProcessing not invoked.");
    }
}
