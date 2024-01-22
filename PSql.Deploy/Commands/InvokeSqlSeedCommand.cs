// Copyright 2024 Subatomix Research Inc.
// SPDX-License-Identifier: ISC

using System.Collections;
using PSql.Deploy.Seeding;

namespace PSql.Deploy.Commands;

/// <summary>
///   The <c>Invoke-SqlSeed</c> cmdlet.
/// </summary>
/// <remarks>
///   Invokes database content seeds against sets of target databases.
/// </remarks>
[Cmdlet(
    VerbsLifecycle.Invoke, "SqlSeed",
    DefaultParameterSetName = ContextParameterSetName,
    SupportsShouldProcess   = true, // -Confirm and -WhatIf
    ConfirmImpact           = ConfirmImpact.High
)]
public class InvokeSqlSeedCommand : PerSqlContextCommand
{
    /// <summary>
    ///   <b>-Seed:</b>
    ///   Names of seeds to apply.
    /// </summary>
    [Parameter]
    [ValidateNotNullOrEmpty]
    public string[] Seed
    {
        get => _seed ??= Array.Empty<string>();
        set => _seed   = value.Sanitize();
    }
    private string[]? _seed;

    /// <summary>
    ///   <b>-Define:</b>
    ///   SQLCMD preprocessor variables to define.
    /// </summary>
    [Parameter]
    [ValidateNotNull]
    [AllowEmptyCollection]
    public Hashtable? Define { get; set; }

    private bool IsWhatIf
        => this.IsWhatIf();

    private string CurrentPath
        => this.GetCurrentPath();

    internal SeedSession.Factory
        SeedSessionFactory { get; set; } = SeedSession.DefaultFactory;

#nullable disable warnings
    // Set in BeginProcessing
    private ISeedSessionControl _session;
#nullable restore

    protected override void BeginProcessingCore()
    {
        var path    = CurrentPath;
        var console = new SeedConsole(this);

        _session              = SeedSessionFactory.Invoke(console, path, CancellationToken);
        _session.IsWhatIfMode = IsWhatIf;

        base.BeginProcessingCore();
    }

    protected override void ProcessRecordCore()
    {
        _session.MaxParallelism = MaxParallelism; // PerDatabase
        _session.DiscoverSeeds(CurrentPath, Seed);

        base.ProcessRecordCore();
    }

    protected override Task ProcessWorkAsync(SqlContextWork work)
    {
        return _session.ApplyAsync(work);
    }
}
