// Copyright Subatomix Research Inc.
// SPDX-License-Identifier: MIT

using System.Diagnostics;
using PSql.Deploy.Seeds;

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
    public SqlTargetDatabaseGroup[]? Target { get; set; }

    /// <summary>
    ///   <b>-Seed:</b>
    ///   Names of seeds to apply.
    /// </summary>
    [Parameter(Position = 1, Mandatory = true, ValueFromPipeline = true)]
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
    ///   <b>-Path:</b>
    ///   Path to a directory containing seeds.
    /// </summary>
    [Parameter]
    [Alias("PSPath", "SourcePath")]
    [ValidateNotNullOrEmpty]
    public string? Path { get; set; }

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
        Assume.NotNull(Seed);

        base.BeginProcessing();

        var currentPath = this.GetCurrentPath();

        _session = new(
            GetOptions(),
            new CmdletSeedConsole(this, currentPath)
        );

        _session.DiscoverSeeds(Path ?? currentPath, Seed);
    }

    protected override void ProcessRecord()
    {
        AssumeBeginProcessingInvoked();

        if (Target is not null)
            foreach (var group in Target)
                if (ShouldProcess(group))
                    _session.BeginApplying(group.InnerGroup);
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
        return new S.SeedSessionOptions
        {
            Defines       = GetDefines(),
            IsWhatIfMode  = this.IsWhatIf(),
            MaxErrorCount = MaxErrorCount,
        };
    }

    private IEnumerable<(string, string)>? GetDefines()
    {
        if (Define is null || Define.Count is 0)
            return [];

        var builder = ImmutableArray.CreateBuilder<(string, string)>(Define.Count);

        foreach (DictionaryEntry entry in Define)
        {
            var key   = entry.Key   .ToString() ?? throw OnNullOrEmptyDefineKey();
            var value = entry.Value?.ToString() ?? "";

            builder.Add((key, value));
        }

        return builder.MoveToImmutable();
    }

    private Exception OnNullOrEmptyDefineKey()
    {
        return new ArgumentException(
            "Key must be non-null and must convert to a non-empty string.",
            nameof(Define)
        );
    }

    private bool ShouldProcess(SqlTargetDatabaseGroup group)
    {
        var action   = $"Applying seed(s) to {group}.";
        var question = $"Apply seed(s) to {group}?";

        return ShouldProcess(action, question, null, out var whyNot)
            || whyNot is ShouldProcessReason.WhatIf;
    }

    [Conditional("DEBUG")]
    [MemberNotNull(nameof(_session))]
    internal void AssumeBeginProcessingInvoked()
    {
        if (_session is null)
            throw new InvalidOperationException("BeginProcessing not invoked.");
    }
}
