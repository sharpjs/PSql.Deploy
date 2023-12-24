// Copyright 2023 Subatomix Research Inc.
// SPDX-License-Identifier: ISC

using System.Collections;
using PSql.Deploy.Seeding;

namespace PSql.Deploy.Commands;

[Cmdlet(
    VerbsLifecycle.Invoke, "SqlSeed",
    DefaultParameterSetName = ContextParameterSetName
)]
public class InvokeSqlSeedCommand : PerSqlContextCommand
{
    // -Seed
    [Parameter]
    [ValidateNotNullOrEmpty]
    public string[] Seed
    {
        get => _seed ??= Array.Empty<string>();
        set => _seed   = value.Sanitize();
    }

    // -Define
    [Parameter]
    [ValidateNotNull]
    [AllowEmptyCollection]
    public Hashtable? Define { get; set; }

    // -WhatIf
    [Parameter()]
    public SwitchParameter WhatIf { get; set; }

    internal SeedSession.Factory
        SeedSessionFactory { get; set; } = SeedSession.DefaultFactory;

    private ISeedSessionControl? _session;
    private string[]? _seed;

    private string CurrentPath => SessionState.Path.CurrentFileSystemLocation.Path;

    protected override void BeginProcessing()
    {
        _session = SeedSessionFactory.Invoke(CurrentPath, CancellationToken);

        base.BeginProcessing();
    }

    protected override void ProcessRecord()
    {
        _session!.IsWhatIfMode = WhatIf;
        _session!.DiscoverSeeds(CurrentPath, Seed);

        base.ProcessRecord();
    }

    protected override Task ProcessWorkAsync(SqlContextWork work)
    {
        return _session!.ApplyAsync(work, this);
    }
}
