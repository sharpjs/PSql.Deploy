// Copyright 2023 Subatomix Research Inc.
// SPDX-License-Identifier: ISC

using PSql.Deploy.Migrations;

namespace PSql.Deploy.Commands;

using static MigrationPhase;

[Cmdlet(VerbsLifecycle.Invoke, "SqlMigrations", DefaultParameterSetName = "Target")]
public class InvokeSqlMigrationsCommand : AsyncCmdlet
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

        foreach (var context in contexts)
            target.Contexts.Add(context);
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
        // ProcessRecord is used to accumulate targets to use with a single
        // migration engine instance.  Therefore, move the async phase into
        // EndProcessing.
        base.ProcessRecord();
    }

    /// <inheritdoc/>
    protected override async Task ProcessRecordAsync(CancellationToken cancellation)
    {
        var path   = SessionState.Path.CurrentFileSystemLocation.ProviderPath;
        var engine = new MigrationEngine(Console, path, cancellation);

        engine.DiscoverMigrations(Path!);
        engine.SpecifyTargets(_targets);

        foreach (var phase in Phase ?? AllPhases)
            await engine.ApplyAsync(phase);
    }
}
