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
    DefaultParameterSetName = ContextParameterSetName
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

    /// <summary>
    ///   <b>-Define:</b>
    ///   SQLCMD preprocessor variables to define.
    /// </summary>
    [Parameter]
    [ValidateNotNull]
    [AllowEmptyCollection]
    public Hashtable? Define { get; set; }

    // TODO: Use SupportsShouldProcess
    [Parameter()]
    public SwitchParameter WhatIf { get; set; }

    internal SeedSession.Factory
        SeedSessionFactory { get; set; } = SeedSession.DefaultFactory;

    private ISeedSessionControl? _session;
    private string[]? _seed;

    private string CurrentPath => SessionState.Path.CurrentFileSystemLocation.Path;

    protected override void BeginProcessing()
    {
        _session = SeedSessionFactory.Invoke(new SeedConsole(this), CurrentPath, CancellationToken);

        base.BeginProcessing();
    }

    protected override void ProcessRecord()
    {
        _session!.IsWhatIfMode   = WhatIf;
        _session!.MaxParallelism = MaxParallelism; // PerDatabase
        _session!.DiscoverSeeds(CurrentPath, Seed);

        base.ProcessRecord();
    }

    protected override Task ProcessWorkAsync(SqlContextWork work)
    {
        return _session!.ApplyAsync(work);
    }
}
