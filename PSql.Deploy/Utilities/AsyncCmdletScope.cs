// Copyright 2023 Subatomix Research Inc.
// SPDX-License-Identifier: ISC

using System.Collections.Concurrent;

namespace PSql.Deploy.Utilities;

/// <summary>
///   A scope in which a cmdlet can invoke asynchronous code.
/// </summary>
internal sealed class AsyncCmdletScope : IDisposable
{
    private readonly Cmdlet                  _cmdlet;
    private readonly ConcurrentBag<Task>     _tasks;
    private readonly MainThreadDispatcher    _dispatcher;
    private readonly CancellationTokenSource _cancellation;
    private readonly SynchronizationContext? _previousContext;

    /// <summary>
    ///   Initializes a new <see cref="AsyncCmdletScope"/> instance for the
    ///   specified cmdlet, with the current thread as the main thread.
    /// </summary>
    /// <param name="cmdlet">
    ///   The cmdlet that will invoke asynchronous code in the scope.
    /// </param>
    /// <exception cref="ArgumentNullException">
    ///   <paramref name="cmdlet"/> is <see langword="null"/>.
    /// </exception>
    public AsyncCmdletScope(Cmdlet cmdlet)
    {
        if (cmdlet is null)
            throw new ArgumentNullException(nameof(cmdlet));

        _cmdlet          = cmdlet;
        _tasks           = new();
        _dispatcher      = new MainThreadDispatcher();
        _cancellation    = new CancellationTokenSource();
        _previousContext = SynchronizationContext.Current;

        // Ensure no synchronization context with conflicting thread mobility ideas
        if (_previousContext is not null)
            SynchronizationContext.SetSynchronizationContext(null);
    }

    /// <summary>
    ///   Gets a dispatcher that forwards invocations to the main thread.
    /// </summary>
    public IDispatcher Dispatcher => _dispatcher;

    /// <summary>
    ///   Gets the token to monitor for cancellation requests.
    /// </summary>
    public CancellationToken CancellationToken => _cancellation.Token;

    /// <summary>
    ///   Queues the specified action to run asynchronously on the thread pool.
    /// </summary>
    /// <param name="action">
    ///   The action to run asynchronously.
    /// </param>
    /// <exception cref="ArgumentNullException">
    ///   <paramref name="action"/> is <see langword="null"/>.
    /// </exception>
    /// <remarks>
    ///   This method is thread-safe.
    /// </remarks>
    public void Run(Func<Task> action)
    {
        if (action is null)
            throw new ArgumentNullException(nameof(action));

        _tasks.Add(Task.Run(action, _cancellation.Token));
    }

    /// <summary>
    ///   Invokes any pending actions dispatched to the main thread.
    /// </summary>
    /// <remarks>
    ///   This method <b>must</b> be invoked on the main thread (the thread
    ///   that constructed the <see cref="AsyncCmdletScope"/> instance).
    /// </remarks>
    public void InvokePendingMainThreadActions()
    {
        _dispatcher.RunPending();
    }

    /// <summary>
    ///   Invokes pending actions dispatched to the main thread, continuously,
    ///   until all asynchronous actions queued by <see cref="Run"/> complete.
    /// </summary>
    /// <remarks>
    ///   <para>
    ///     This method <b>must</b> be invoked on the main thread (the thread
    ///     that constructed the <see cref="AsyncCmdletScope"/> instance).
    ///   </para>
    ///   <para>
    ///     This method may be invoked only once per
    ///     <see cref="AsyncCmdletScope"/> instance.
    ///   </para>
    /// </remarks>
    public void Complete()
    {
        InvokePendingMainThreadActions();

        if (_tasks.Count == 0)
            return;

        var task = Task
            .WhenAll(_tasks)
            .ContinueWith(_ => _dispatcher.Complete());

        _dispatcher.Run();

        task.GetAwaiter().GetResult();
    }

    /// <summary>
    ///   Requests cancellation of any asynchronous actions queued by
    ///   <see cref="Run"/>.
    /// </summary>
    public void Cancel(bool silent = false)
    {
        if (!silent)
            _cmdlet.WriteHost("Canceling...");

        _cancellation.Cancel();
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        // Restore previous synchronization context
        if (_previousContext is not null)
            SynchronizationContext.SetSynchronizationContext(_previousContext);

        _cancellation.Dispose();
        _dispatcher  .Dispose();
    }
}
