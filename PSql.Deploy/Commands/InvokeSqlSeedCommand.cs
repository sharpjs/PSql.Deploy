// Copyright Subatomix Research Inc.
// SPDX-License-Identifier: MIT

using System.Diagnostics;
using PSql.Deploy.Seeding;

namespace PSql.Deploy.Commands;

/// <summary>
///   The <c>Invoke-SqlSeed</c> cmdlet.
/// </summary>
/// <remarks>
///   Invokes database content seeds against sets of target databases.
/// </remarks>
[Cmdlet(VerbsLifecycle.Invoke, "SqlSeed",
    SupportsShouldProcess = true, // -Confirm and -WhatIf
    ConfirmImpact         = ConfirmImpact.High
)]
public class InvokeSqlSeedCommand : AsyncPSCmdlet
{
    /// <summary>
    ///   <b>-Target:</b>
    ///   Objects specifying how to connect to the databases in the target set.
    /// </summary>
    [Parameter(Position = 0, Mandatory = true, ValueFromPipeline = true)]
    [ValidateNotNullOrEmpty]
    public TargetSet[]? Target { get; set; }

    /// <summary>
    ///   <b>-Seed:</b>
    ///   Names of seeds to apply.
    /// </summary>
    [Parameter]
    [ValidateNotNullOrEmpty]
    public string[]? Seed { get; set; }

    /// <summary>
    ///   <b>-Define:</b>
    ///   SQLCMD preprocessor variables to define.
    /// </summary>
    [Parameter]
    [ValidateNotNull]
    [AllowEmptyCollection]
    public Hashtable? Define { get; set; }

    /// <summary>
    ///   <b>-MaxErrorCount:</b>
    ///   Maximum count of errors to allow.  If the count of errors exceeds
    ///   this value, the command attempts to cancel in-progress operations and
    ///   terminates early.
    /// </summary>
    [Parameter()]
    [ValidateRange(0, int.MaxValue)]
    public int MaxErrorCount { get; set; }

    private S.SeedSession? _session;

    protected override void BeginProcessing()
    {
        base.BeginProcessing();

        _session = new(
            GetOptions(),
            new CmdletSeedConsole(this, this.GetCurrentPath())
        );
    }

    protected override void ProcessRecord()
    {
        AssumeBeginProcessingInvoked();

        if (Target is not null)
            foreach (var target in Target)
                if (target is not null)
                    _session.BeginApplying(target.InnerTargetSet);
    }

    protected override void EndProcessing()
    {
        AssumeBeginProcessingInvoked();

        Run(() => _session.CompleteApplyingAsync(CancellationToken));

        base.EndProcessing();
    }

    protected override void Dispose(bool managed)
    {
        if (managed)
        {
            _session?.Dispose();
            _session = null;
        }

        base.Dispose(managed);
    }

    private S.SeedSessionOptions GetOptions()
    {
        var options = default(S.SeedSessionOptions);

        if (this.IsWhatIf())
            options |= S.SeedSessionOptions.IsWhatIfMode;

        return options;
    }

    [Conditional("DEBUG")]
    [MemberNotNull(nameof(_session))]
    private void AssumeBeginProcessingInvoked()
    {
        if (_session is null)
            throw new InvalidCastException("BeginProcessing not invoked.");
    }
}
