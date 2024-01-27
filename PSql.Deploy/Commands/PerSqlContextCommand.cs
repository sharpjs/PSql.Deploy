// Copyright 2024 Subatomix Research Inc.
// SPDX-License-Identifier: ISC

using System.Collections.Concurrent;
using Subatomix.PowerShell.TaskHost;

namespace PSql.Deploy.Commands;

/// <summary>
///   Base class for cmdlets that operate on multiple databases in parallel.
/// </summary>
public abstract class PerSqlContextCommand : AsyncPSCmdlet
{
    internal const string
        ContextSetParameterSetName = nameof(ContextSet),
        ContextParameterSetName    = nameof(Context);

    /// <summary>
    ///   <b>-ContextSet:</b>
    ///   Objects specifying sets of databases on which to operate with limited
    ///   parallelism.  Obtain via the <c>New-SqlContextParallelSet</c> cmdlet.
    /// </summary>
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
    private SqlContextParallelSet[]? _targets;

    /// <summary>
    ///   <b>-Context:</b>
    ///   Objects specifying how to connect to databases.  Obtain via the
    ///   <c>New-SqlContext</c> cmdlet.
    /// </summary>
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
    private SqlContext[]? _contexts;

    /// <summary>
    ///   <b>-MaxParallelism:</b>
    ///   Maximum count of operations to perform in parallel.  The default
    ///   value is the number of logical processors on the local machine.
    /// </summary>
    [Parameter(ParameterSetName = ContextParameterSetName)]
    [ValidateRange(1, int.MaxValue)]
    public int MaxParallelism
    {
        get => SqlContextParallelSet.GetMaxParallelism(ref _maxParallelism);
        set => SqlContextParallelSet.SetMaxParallelism(ref _maxParallelism, value);
    }
    private int _maxParallelism;

    /// <summary>
    ///   <b>-MaxErrors:</b>
    ///   Maximum count of errors to allow.  If the count exceeds this value,
    ///   the command attempts to cancel in-progress operations and terminate
    ///   early.
    /// </summary>
    [Parameter()]
    [ValidateRange(0, int.MaxValue)]
    public int MaxErrorCount
    {
        get => _maxErrorCount;
        set => _maxErrorCount = value >= 0
            ? value
            : throw new ArgumentOutOfRangeException(nameof(value));
    }
    private int _maxErrorCount = int.MaxValue;

    // Accumulated errors
    private readonly ConcurrentQueue<Exception> _exceptions = new();

    /// <inheritdoc/>
    protected sealed override void BeginProcessing()
    {
        if (TaskHost.Current is null)
            return;

        BeginProcessingCore();
    }

    /// <summary>
    ///   Performs execution of the command.
    /// </summary>
    protected override void ProcessRecord()
    {
        if (TaskHost.Current is null)
            ReinvokeWithTaskHost();
        else
            ProcessRecordCore();
    }

    /// <inheritdoc/>
    protected sealed override void EndProcessing()
    {
        if (TaskHost.Current is null)
            return;

        EndProcessingCore();
    }

    private void ReinvokeWithTaskHost()
    {
        using var invocation = new Invocation();

        invocation
            .AddReinvocation(MyInvocation)
            .UseTaskHost(this, withElapsed: true)
            .Invoke();
    }

    /// <inheritdoc cref="BeginProcessing"/>
    protected virtual void BeginProcessingCore()
    {
        base.BeginProcessing();
    }

    /// <inheritdoc cref="ProcessRecord"/>
    protected virtual void ProcessRecordCore()
    {
        if (ParameterSetName == ContextParameterSetName)
            ProcessContextSet(MakeContextSet());
        else
            foreach (var contextSet in ContextSet)
                ProcessContextSet(contextSet);
    }

    /// <inheritdoc cref="EndProcessing"/>
    protected virtual void EndProcessingCore()
    {
        base.EndProcessing();

        ThrowAccumulatedErrors();
    }

    private SqlContextParallelSet MakeContextSet()
    {
        return new()
        {
            Contexts                  = Context!,
            MaxParallelism            = MaxParallelism,
            MaxParallelismPerDatabase = MaxParallelism
        };
    }

    protected virtual void ProcessContextSet(SqlContextParallelSet contextSet)
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
            initialCount: contextSet.MaxParallelism,
            maxCount:     contextSet.MaxParallelism
        );

        Task ProcessAsync(SqlContext context)
            => ProcessContextAsync(context, limiter);

        await Task.WhenAll(contextSet.Contexts.Select(ProcessAsync));
    }

    private async Task ProcessContextAsync(SqlContext context, SemaphoreSlim limiter)
    {
        // Move to another thread so that caller's context iterator continues
        try
        {
            await Task.Yield();
        }
        catch (OperationCanceledException)
        {
            // Not an error
            return;
        }

        // Limit parallelism
        await limiter.WaitAsync(CancellationToken);
        try
        {
            var work = new SqlContextWork(context);

            using (TaskScope.Begin(work.DatabaseDisplayName))
                await ProcessWorkAsync(work);
        }
        catch (OperationCanceledException)
        {
            // Not an error
        }
        catch (Exception e)
        {
            _exceptions.Enqueue(e);

            if (_exceptions.Count > MaxErrorCount)
                StopProcessing();
        }
        finally
        {
            limiter.Release();
        }
    }

    protected abstract Task ProcessWorkAsync(SqlContextWork work);

    private void ThrowAccumulatedErrors()
    {
        if (GetAccumulatedErrors() is not { } exception)
            return;

        ThrowTerminatingError(new(
            exception, "", ErrorCategory.OperationStopped, null!
        ));
    }

    private Exception? GetAccumulatedErrors()
    {
        return _exceptions.Count switch
        {
            0 => null,
            1 => _exceptions.First(),
            _ => new AggregateException(_exceptions),
        };
    }
}
