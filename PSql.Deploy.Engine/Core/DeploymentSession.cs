// Copyright Subatomix Research Inc.
// SPDX-License-Identifier: MIT

using System.Collections.Concurrent;

namespace PSql.Deploy;

/// <summary>
///   A database deployment session.  Base class of
///   <see cref="Migrations.MigrationSession"/> and
///   <see cref="Seeds.SeedSession"/>.
/// </summary>
public abstract class DeploymentSession : IDeploymentSessionInternal
{
    private readonly ConcurrentBag<Task>        _tasks;
    private readonly ConcurrentQueue<Exception> _exceptions;
    private readonly CancellationTokenSource    _cancellation;

    /// <summary>
    ///   Initializes a new <see cref="DeploymentSession"/> instance.
    /// </summary>
    /// <param name="options">
    ///   Options for the session.
    /// </param>
    /// <exception cref="ArgumentNullException">
    ///   <paramref name="options"/> is <see langword="null"/>.
    /// </exception>
    protected DeploymentSession(DeploymentSessionOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        _tasks        = [];
        _exceptions   = [];
        _cancellation = new();

        IsWhatIfMode  = options.IsWhatIfMode;
        MaxErrorCount = options.MaxErrorCount;

        Parallelism   = new GlobalParallelism(
            InterpretParallelism(options.MaxParallelism),
            InterpretParallelism(options.MaxParallelismPerTarget)
        );
    }

    /// <summary>
    ///   Gets the token to monitor for cancellation requests.
    /// </summary>
    public CancellationToken CancellationToken => _cancellation.Token;

    /// <inheritdoc/>
    public bool IsWhatIfMode { get; }

    /// <summary>
    ///   Gets the parallelism policy for the session.
    /// </summary>
    private GlobalParallelism Parallelism { get; }

    /// <inheritdoc/>
    public int MaxParallelism => Parallelism.MaxActions;

    /// <inheritdoc/>
    public int MaxParallelismPerTarget => Parallelism.MaxActionsPerTarget;

    /// <inheritdoc/>
    public int MaxErrorCount { get; }

    /// <inheritdoc/>
    public bool HasErrors => !_exceptions.IsEmpty;

    /// <inheritdoc/>
    public virtual void BeginApplying(TargetGroup group)
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

        BeginApplying(new TargetGroup([target], name: null, maxParallelism, maxParallelism));
    }

    /// <inheritdoc/>
    public virtual async Task CompleteApplyingAsync(CancellationToken cancellation = default)
    {
        using var _ = cancellation.Register(_cancellation.Cancel, useSynchronizationContext: false);

        try
        {
            await Task.WhenAll(_tasks).ConfigureAwait(continueOnCapturedContext: false);
            ThrowAccumulatedErrors();
        }
        catch (OperationCanceledException)
        {
            // If the session has accumulated other exceptions, throw those instead
            ThrowAccumulatedErrors();
            throw;
        }
        finally
        {
            _tasks     .Clear();
            _exceptions.Clear();
        }
    }

    /// <inheritdoc/>
    public void Cancel()
    {
        _cancellation.Cancel();
    }

    /// <inheritdoc/>
    public virtual void Dispose()
    {
        // NOTE: Unmanaged disposal not required
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
        using var parallelism = Parallelism.ForGroup(group, GetMaxParallelTargets(group));

        Task ApplyToTargetAsync(Target target)
            => ApplyAsync(target, parallelism);

        await Task.WhenAll(group.Targets.Select(ApplyToTargetAsync));
    }

    private async Task ApplyAsync(Target target, TargetGroupParallelism parallelism)
    {
        // Move to another thread so that caller's target iterator continues
        await Task.Yield();

        try
        {
            // Limit max parallel targets in group
            using var _ = await parallelism.BeginTargetScopeAsync(CancellationToken);

            await ApplyCoreAsync(target, parallelism.ForTarget);
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
    ///   Gets the number of target databases against which the deployment
    ///   operation should run in parallel for the specified target group.
    /// </summary>
    /// <param name="group">
    ///   The group of target databases.
    /// </param>
    /// <returns>
    ///   The count of target databases in <paramref name="group"/> against
    ///   which the deployment operation should run in parallel.
    /// </returns>
    protected abstract int GetMaxParallelTargets(TargetGroup group);

    /// <summary>
    ///   Applies the deployment operation to the specified target database
    ///   asynchronously.
    /// </summary>
    /// <param name="target">
    ///   An object specifying the target database.
    /// </param>
    /// <param name="parallelism">
    ///   The policy to manage parallelism of actions against the target
    ///   database.
    /// </param>
    /// <returns>
    ///   A <see cref="Task"/> representing the asynchronous operation.
    /// </returns>
    protected abstract Task ApplyCoreAsync(Target target, TargetParallelism parallelism);

    private void HandleError(Exception e, Target target)
    {
        if (e.Data is { IsReadOnly: false } data)
            data[nameof(Target)] = target.FullDisplayName;

        _exceptions.Enqueue(e);

        if (_exceptions.Count > MaxErrorCount)
            Cancel();
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

    /// <summary>
    ///   Applies defaulting for parallelism values.
    /// </summary>
    /// <param name="value">
    ///   The desired degree of parallelism.
    /// </param>
    /// <returns>
    ///   The level of parallelism to use: <paramref name="value"/> if greater
    ///   than zero; otherwise, the number of logical processors on the local
    ///   machine.
    /// </returns>
    internal static int InterpretParallelism(int value)
        => value > 0 ? value : ProcessInfo.Instance.ProcessorCount;
}
