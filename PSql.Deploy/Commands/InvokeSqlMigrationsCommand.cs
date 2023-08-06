// Copyright 2023 Subatomix Research Inc.
// SPDX-License-Identifier: ISC

using PSql.Deploy.Migrations;
using PSql.Deploy.Utilities;

namespace PSql.Deploy.Commands;

using static MigrationPhase;

[Cmdlet(VerbsLifecycle.Invoke, "SqlMigrations", DefaultParameterSetName = "Target")]
public class InvokeSqlMigrationsCommand : Cmdlet, IAsyncCmdlet
{
    // -Path
    [Parameter(Mandatory = true, Position = 0)]
    [Alias("PSPath")]
    [ValidateNotNullOrEmpty]
    public string? Path { get; set; }

    // -Target
    [Parameter(Mandatory = true, Position = 1, ValueFromPipeline = true, ParameterSetName = "Target")]
    [ValidateNotNullOrEmpty]
    public SqlContextParallelSet[]? Target { get; set; }

    // -Context
    [Parameter(Mandatory = true, Position = 1, ValueFromPipeline = true, ParameterSetName = "Context")]
    [ValidateNotNullOrEmpty]
    public SqlContext[]? Context { get; set; }

    // -Parallelism
    [Parameter(ParameterSetName = "Context")]
    [Alias("ThrottleLimit")]
    [ValidateRange(1, int.MaxValue)]
    public int Parallelism { get; set; }

    // -Phase
    [Parameter()]
    [ValidateNotNullOrEmpty]
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

    public List<SqlContextParallelSet> _targets = new();

    protected override void ProcessRecord()
    {
        if (ParameterSetName == "Target")
            ProcessTargets(Target!);
        else
            ProcessContexts(Context!);
    }

    private void ProcessTargets(SqlContextParallelSet[] sets)
    {
        _targets.AddRange(sets);
    }

    private void ProcessContexts(SqlContext[] contexts)
    {
        var target = EnsureTarget();

        // TODO: Figure out better way
        target.Contexts = contexts;

        //foreach (var context in contexts)
        //    target.Contexts.Add(context);
    }

    private SqlContextParallelSet EnsureTarget()
    {
        if (_targets.Count > 0)
            return _targets[0];

        var target = new SqlContextParallelSet();

        _targets.Add(target);

        if (Parallelism > 0)
            target.Parallelism = Parallelism;

        return target;
    }

    protected override void EndProcessing()
    {
#if PREVIOUS
        using var scope = new AsyncCmdletScope(this);

        scope.Run(ProcessAsync);
#endif
    }

#if PREVIOUS
    private async Task ProcessAsync(IAsyncCmdletContext context)
    {
        var path   = SessionState.Path.CurrentFileSystemLocation.ProviderPath;
        var engine = MigrationEngineFactory.Create(console: this, path, context.CancellationToken);

        engine.DiscoverMigrations(Path!, MaximumMigrationName);
        engine.SpecifyTargets(_targets);

        engine.AllowCorePhase = AllowCorePhase.IsPresent;
        engine.IsWhatIfMode   = WhatIf        .IsPresent;

        foreach (var phase in Phase ?? AllPhases)
            await engine.ApplyAsync(phase);
    }
#endif
}
