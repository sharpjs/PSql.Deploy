// Copyright 2023 Subatomix Research Inc.
// SPDX-License-Identifier: ISC

using Subatomix.PowerShell.TaskHost;

namespace PSql.Deploy.Commands;

public abstract class PerSqlContextCommand : AsyncPSCmdlet
{
    internal const string
        ContextSetParameterSetName = nameof(ContextSet),
        ContextParameterSetName    = nameof(Context);

    private SqlContextParallelSet[]? _targets;
    private SqlContext[]?            _contexts;
    private int                      _parallelism;

    // -ContextSet
    [Parameter(
        Position          = 0,
        Mandatory         = true,
        ValueFromPipeline = true,
        ParameterSetName  = ContextSetParameterSetName
    )]
    [ValidateNotNullOrEmpty]
    public SqlContextParallelSet[] ContextSet
    {
        get => _targets ??= Array.Empty<SqlContextParallelSet>();
        set => _targets   = value.Sanitize();
    }

    // -Context
    [Parameter(
        Position          = 0,
        ValueFromPipeline = true,
        ParameterSetName  = ContextParameterSetName
    )]
    [ValidateNotNullOrEmpty]
    public SqlContext[] Context
    {
        get => _contexts ??= Array.Empty<SqlContext>();
        set => _contexts   = value.Sanitize();
    }

    // -Parallelism
    [Parameter(ParameterSetName = ContextParameterSetName)]
    [Alias("ThrottleLimit")]
    [ValidateRange(1, int.MaxValue)]
    public int Parallelism
    {
        get => _parallelism > 0 ? _parallelism : _parallelism = Environment.ProcessorCount;
        set
        {
            if (value <= 0)
                throw new ArgumentOutOfRangeException(nameof(value));

            _parallelism = value;
        }
    }

    protected override void ProcessRecord()
    {
        if (CancellationToken.IsCancellationRequested)
            return;

        if (ParameterSetName == ContextParameterSetName)
            ProcessContextSet(new() { Contexts = Context!, Parallelism = Parallelism });
        else
            foreach (var contextSet in ContextSet)
                ProcessContextSet(contextSet);
    }

    private void ProcessContextSet(SqlContextParallelSet contextSet)
    {
        if (contextSet.Contexts.Count == 0)
            return;

        Task ProcessAsync()
            => ProcessContextSetAsync(contextSet);

        Run(ProcessAsync);
    }

    private async Task ProcessContextSetAsync(SqlContextParallelSet contextSet)
    {
        using var limiter = new SemaphoreSlim(
            initialCount: contextSet.Parallelism,
            maxCount:     contextSet.Parallelism
        );

        Task ProcessAsync(SqlContext context)
            => ProcessContextAsync(context, limiter);

        await Task.WhenAll(contextSet.Contexts.Select(ProcessAsync));
    }

    private async Task ProcessContextAsync(SqlContext context, SemaphoreSlim limiter)
    {
        // Move to another thread so that caller's context iterator continues
        await Task.Yield();

        // Limit parallelism
        await limiter.WaitAsync(CancellationToken);
        try
        {
            var work = new SqlContextWork(context);

            using (TaskScope.Begin(work.DatabaseDisplayName))
                await ProcessWorkAsync(work);
        }
        finally
        {
            limiter.Release();
        }
    }

    protected abstract Task ProcessWorkAsync(SqlContextWork work);
}
