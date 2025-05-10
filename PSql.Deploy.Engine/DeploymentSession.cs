// Copyright Subatomix Research Inc.
// SPDX-License-Identifier: MIT

using System.Collections.Concurrent;

namespace PSql.Deploy;

/// <summary>
///   A database deployment session.  Base class of
///   <see cref="Migrations.MigrationSession"/> and
///   <see cref="Seeding.SeedSession"/>.
/// </summary>
public abstract class DeploymentSession : IDeploymentSessionInternal
{
    private readonly ConcurrentBag<Task>        _tasks;
    private readonly ConcurrentQueue<Exception> _exceptions;
    private readonly CancellationTokenSource    _cancellation;

    /// <summary>
    ///   Initializes a new <see cref="DeploymentSession"/> instance.
    /// </summary>
    /// <param name="maxErrorCount">
    ///   The maximum count of exceptions that the session should tolerate
    ///   before cancelling ongoing operations.  Must be a positive number.
    /// </param>
    /// <exception cref="ArgumentOutOfRangeException">
    ///   <paramref name="maxErrorCount"/> is <c>0</c> or negative.
    /// </exception>
    private protected DeploymentSession(int maxErrorCount = 1)
    {
        if (maxErrorCount < 1)
            throw new ArgumentOutOfRangeException(nameof(maxErrorCount));

        _tasks        = [];
        _exceptions   = [];
        _cancellation = new();

        MaxErrorCount = maxErrorCount;
    }

    /// <summary>
    ///   Gets the token to monitor for cancellation requests.
    /// </summary>
    public CancellationToken CancellationToken => _cancellation.Token;

    /// <summary>
    ///   Gets the maximum count of exceptions that the session will tolerate
    ///   before cancelling ongoing operations.
    /// </summary>
    public int MaxErrorCount { get; }

    /// <inheritdoc/>
    public abstract bool IsWhatIfMode { get; }

    /// <inheritdoc/>
    public bool HasErrors => !_exceptions.IsEmpty;

    /// <inheritdoc/>
    public void BeginApplying(TargetGroup group)
    {
        if (group is null)
            throw new ArgumentNullException(nameof(group));

        Run(() => ApplyAsync(group));
    }

    /// <inheritdoc/>
    public void BeginApplying(Target target, int maxParallelism = 0)
    {
        if (target is null)
            throw new ArgumentNullException(nameof(target));
        if (maxParallelism < 0)
            throw new ArgumentOutOfRangeException(nameof(maxParallelism));
        if (maxParallelism == 0)
            maxParallelism = ProcessInfo.Instance.ProcessorCount;

        Run(() => ApplyAsync(target, maxParallelism));
    }

    /// <inheritdoc/>
    public async Task CompleteApplyingAsync(CancellationToken cancellation = default)
    {
        using var _ = cancellation.Register(_cancellation.Cancel, useSynchronizationContext: false);

        await Task.WhenAll(_tasks).ConfigureAwait(continueOnCapturedContext: false);

        ThrowAccumulatedErrors();
    }

    /// <inheritdoc/>
    public virtual void Dispose()
    {
        _cancellation.Dispose();
        GC.SuppressFinalize(this);
    }

    private void Run(Func<Task> action)
    {
        // NOTE: The action runs without a current SynchronizationContext, so
        // ConfigureAwait(continueOnCapturedContext: false) is unnecessary.
        // Source: https://learn.microsoft.com/en-us/archive/msdn-magazine/2015/july/async-programming-brownfield-async-development#the-thread-pool-hack

        _tasks.Add(Task.Run(action, CancellationToken));
    }

    private async Task ApplyAsync(TargetGroup group)
    {
        using var limiter = new SemaphoreSlim(
            initialCount: group.MaxParallelism,
            maxCount:     group.MaxParallelism
        );

        Task ApplyToTargetAsync(Target context)
            => ApplyAsync(context, limiter, group.MaxParallelismPerDatabase);

        await Task.WhenAll(group.Targets.Select(ApplyToTargetAsync));
    }

    private async Task ApplyAsync(Target target, SemaphoreSlim limiter, int maxParallelism)
    {
        // Move to another thread so that caller's context iterator continues
        await Task.Yield();

        var limited = false;

        try
        {
            // Limit group parallelism
            await limiter.WaitAsync(CancellationToken);
            limited = true;

            await ApplyAsync(target, maxParallelism);
        }
        finally
        {
            if (limited)
                limiter.Release();
        }
    }

    private async Task ApplyAsync(Target target, int maxParallelism)
    {
        try
        {
            await ApplyCoreAsync(target, maxParallelism);
        }
        catch (OperationCanceledException)
        {
            // Not an error
        }
        catch (Exception e)
        {
            HandleError(e, target);
        }
    }

    /// <summary>
    ///   Applies the deployment operation to the specified target database
    ///   asynchronously.
    /// </summary>
    /// <param name="target">
    ///   An object specifying the target database.
    /// </param>
    /// <param name="maxParallelism">
    ///   The maximum degree of parallelism to use.
    /// </param>
    /// <returns>
    ///   A <see cref="Task"/> representing the asynchronous operation.
    /// </returns>
    protected abstract Task ApplyCoreAsync(Target target, int maxParallelism);

    private void HandleError(Exception e, Target? target)
    {
        if (e.Data is { IsReadOnly: false } data && target is { })
            data[nameof(Target)] = target;

        _exceptions.Enqueue(e);

        if (_exceptions.Count > MaxErrorCount)
            _cancellation.Cancel();
    }

    private void ThrowAccumulatedErrors()
    {
        if (GetAccumulatedErrors() is not { } exception)
            return;

        throw Transform(exception);
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

    /// <summary>
    ///   Transforms the specified exception to a form appropriate for the
    ///   deployment operation.
    /// </summary>
    /// <param name="exception">
    ///   The exception to transform.
    /// </param>
    protected virtual Exception Transform(Exception exception)
    {
        return exception;
    }
}
