// Copyright Subatomix Research Inc.
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
        get => _targets ??= [];
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
        get => _contexts ??= [];
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
    ///   <b>-MaxErrorCount:</b>
    ///   Maximum count of errors to allow.  If the count of errors exceeds
    ///   this value, the command attempts to cancel in-progress operations and
    ///   terminates early.
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
    private int _maxErrorCount;

    // Accumulated errors
    private readonly ConcurrentQueue<Exception> _exceptions = new();

    /// <summary>
    ///   Performs initialization of command execution.
    /// </summary>
    /// <remarks>
    ///   If a <see cref="TaskHost"/> is available, this method invokes
    ///   <see cref="BeginProcessingCore"/>.  Otherwise, this method does
    ///   nothing and assumes that <see cref="ProcessRecord"/> will reinvoke
    ///   the current command with an active <see cref="TaskHost"/>.
    /// </remarks>
    protected sealed override void BeginProcessing()
    {
        if (TaskHost.Current is null)
            return;

        BeginProcessingCore();
    }

    /// <summary>
    ///   Performs execution of the command.
    /// </summary>
    /// <remarks>
    ///   If a <see cref="TaskHost"/> is available, this method invokes
    ///   <see cref="ProcessRecordCore"/>.  Otherwise, this method reinvokes
    ///   the current command with an active <see cref="TaskHost"/>.
    /// </remarks>
    protected sealed override void ProcessRecord()
    {
        if (TaskHost.Current is null)
            ReinvokeWithTaskHost();
        else
            ProcessRecordCore();
    }

    /// <summary>
    ///   Performs cleanup after command execution.
    /// </summary>
    /// <remarks>
    ///   If a <see cref="TaskHost"/> is available, this method invokes
    ///   <see cref="EndProcessingCore"/>.  Otherwise, this method does nothing
    ///   and assumes that <see cref="ProcessRecord"/> will reinvoke the
    ///   current command with an active <see cref="TaskHost"/>.
    /// </remarks>
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

    /// <inheritdoc cref="AsyncPSCmdlet.BeginProcessing"/>
    protected virtual void BeginProcessingCore()
    {
        base.BeginProcessing();
    }

    /// <summary>
    ///   Performs execution of the command.
    /// </summary>
    /// <remarks>
    ///   This implementation invokes <see cref="ProcessContextSet"/> with each
    ///   <see cref="SqlContextParallelSet"/> specified by the current
    ///   parameter values.
    /// </remarks>
    protected virtual void ProcessRecordCore()
    {
        InvokePendingMainThreadActions();

        if (ParameterSetName is ContextParameterSetName)
            ProcessContextSet(MakeContextSet());
        else
            foreach (var contextSet in ContextSet)
                ProcessContextSet(contextSet);
    }

    /// <inheritdoc cref="AsyncPSCmdlet.EndProcessing"/>
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

    /// <summary>
    ///   Performs execution of the command for the specified set of databases.
    /// </summary>
    /// <param name="contextSet">
    ///   An object specifying a set of databases on which to operate with
    ///   limited parallelism.
    /// </param>
    /// <remarks>
    ///   This method queues processing of <paramref name="contextSet"/> on the
    ///   thread pool and returns immediately.
    /// </remarks>
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
        await Task.Yield();

        var limited = false;
        var work    = null as SqlContextWork;

        try
        {
            // Limit parallelism
            await limiter.WaitAsync(CancellationToken);
            limited = true;

            // Describe work
            work = new(context);

            // Process work
            using (TaskScope.Begin(work.DatabaseDisplayName))
                await ProcessWorkAsync(work);
        }
        catch (OperationCanceledException)
        {
            // Not an error
        }
        catch (Exception e)
        {
            HandleError(e, work);
        }
        finally
        {
            if (limited)
                limiter.Release();
        }
    }

    /// <summary>
    ///   Performs execution of the command for the specified database
    ///   asynchronously.
    /// </summary>
    /// <param name="work">
    ///   An object specifying a database on which to operate.
    /// </param>
    /// <returns>
    ///   A <see cref="Task"/> representing the asynchronous operation.
    /// </returns>
    protected abstract Task ProcessWorkAsync(SqlContextWork work);

    private void HandleError(Exception e, SqlContextWork? work)
    {
        if (e.Data is { IsReadOnly: false } data && work is { })
            data[nameof(SqlContextWork)] = work;

        _exceptions.Enqueue(e);

        if (_exceptions.Count > MaxErrorCount)
            StopProcessing();
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

    private void ThrowAccumulatedErrors()
    {
        if (GetAccumulatedErrors() is not { } exception)
            return;

        ThrowTerminatingError(new(
            Transform(exception), "", ErrorCategory.OperationStopped, null!
        ));
    }

    /// <summary>
    ///   Transforms the specified exception for subsequent throwing as a
    ///   command-terminating error.
    /// </summary>
    /// <param name="exception">
    ///   The exception to transform.
    /// </param>
    protected virtual Exception Transform(Exception exception)
    {
        return exception;
    }
}
