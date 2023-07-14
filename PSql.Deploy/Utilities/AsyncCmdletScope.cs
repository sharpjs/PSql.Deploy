// Copyright 2023 Subatomix Research Inc.
// SPDX-License-Identifier: ISC

namespace PSql.Deploy.Utilities;

/// <summary>
///   A scope in which a cmdlet can invoke asynchronous code.
/// </summary>
internal sealed class AsyncCmdletScope : IAsyncCmdletContext, IDisposable
{
    private readonly IAsyncCmdlet                _cmdlet;
    private readonly MainThreadDispatcher        _dispatcher;
    private readonly DispatchedCommandRuntime    _runtime;
    private readonly CancellationTokenSource     _cancellation;
    private readonly ConsoleCancellationListener _listener;
    private readonly SynchronizationContext?     _previousContext;

    /// <summary>
    ///   Initializes a new <see cref="AsyncCmdletScope"/> instance for the
    ///   specified cmdlet.
    /// </summary>
    /// <param name="cmdlet">
    ///   The cmdlet that will invoke asynchronous code in the scope.
    /// </param>
    /// <exception cref="ArgumentNullException">
    ///   <paramref name="cmdlet"/> is <see langword="null"/>.
    /// </exception>
    public AsyncCmdletScope(IAsyncCmdlet cmdlet)
    {
        if (cmdlet is null)
            throw new ArgumentNullException(nameof(cmdlet));

        _cmdlet          = cmdlet;
        _dispatcher      = new MainThreadDispatcher();
        _runtime         = new DispatchedCommandRuntime(cmdlet.CommandRuntime, _dispatcher);
        _cancellation    = new CancellationTokenSource();
        _listener        = new ConsoleCancellationListener(cmdlet, _cancellation);
        _previousContext = SynchronizationContext.Current;

        // Replace runtime with one that (re)dispatches actions on the main thread
        cmdlet.CommandRuntime = _runtime;

        // Ensure no synchronization context with conflicting thread mobility ideas
        if (_previousContext is not null)
            SynchronizationContext.SetSynchronizationContext(null);
    }

    /// <inheritdoc/>
    public IDispatcher Dispatcher => _dispatcher;

    /// <inheritdoc/>
    public CancellationToken CancellationToken => _cancellation.Token;

    /// <summary>
    ///   Invokes the specified asynchronous code and waits for it to complete.
    /// </summary>
    /// <param name="action">
    ///   The asynchronous code to invoke.
    /// </param>
    /// <remarks>
    ///   This method may be used only once per <see cref="AsyncCmdletScope"/>
    ///   instance.
    /// </remarks>
    /// <exception cref="ArgumentNullException">
    ///   <paramref name="action"/> is <see langword="null"/>.
    /// </exception>
    public void Run(Func<IAsyncCmdletContext, Task> action)
    {
        if (action is null)
            throw new ArgumentNullException(nameof(action));

        async Task RunAsync()
        {
            try
            {
                await action(this);
            }
            finally
            {
                _dispatcher.End();
            }
        }

        var task = Task.Run(RunAsync);

        _dispatcher.Run();

        task.GetAwaiter().GetResult();
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        // Restore previous synchronization context
        if (_previousContext is not null)
            SynchronizationContext.SetSynchronizationContext(_previousContext);

        // Restore previous command runtime
        _cmdlet.CommandRuntime = _runtime.UnderlyingCommandRuntime;

        _listener    .Dispose();
        _cancellation.Dispose();
        _dispatcher  .Dispose();
    }
}
