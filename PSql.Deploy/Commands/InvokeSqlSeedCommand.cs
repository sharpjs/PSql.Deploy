// Copyright Subatomix Research Inc.
// SPDX-License-Identifier: MIT

using System.Collections;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
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
    [TransformToTargetSet]
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

    private SeedSession? _session;

    protected override void BeginProcessing()
    {
        base.BeginProcessing();

        _session = new SeedSession();
    }

    protected override void ProcessRecord()
    {
        AssumeBeginProcessingInvoked();

        if (Target is not null)
            foreach (var target in Target)
                if (target is not null)
                    _session.BeginApplying(target);
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

    [Conditional("DEBUG")]
    [MemberNotNull(nameof(_session))]
    private void AssumeBeginProcessingInvoked()
    {
        if (_session is null)
            throw new InvalidCastException("BeginProcessing not invoked.");
    }

#if CONVERTED
    internal SeedSession.Factory
        SeedSessionFactory { get; set; } = SeedSession.DefaultFactory;

    private ISeedSessionControl? _session;

    /// <inheritdoc/>
    protected override void BeginProcessingCore()
    {
        var path    = this.GetCurrentPath();
        var console = new SeedConsole(this);

        _session                = SeedSessionFactory.Invoke(console, path, CancellationToken);
        _session.IsWhatIfMode   = this.IsWhatIf();
        _session.MaxParallelism = MaxParallelism; // PerDatabase
        _session.DiscoverSeeds(path, Seed);

        base.BeginProcessingCore();
    }

    /// <inheritdoc/>
    protected override Task ProcessWorkAsync(SqlContextWork work)
    {
        AssertInitialized();

        return _session.ApplyAsync(work);
    }

    /// <inheritdoc/>
    protected override Exception Transform(Exception exception)
    {
        return exception as SeedException
            ?? new SeedException(null, exception);
    }

    [Conditional("DEBUG")]
    [MemberNotNull(nameof(_session))]
    private void AssertInitialized()
    {
        Debug.Assert(_session != null);
    }
#endif
}
