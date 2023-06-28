// Copyright 2023 Subatomix Research Inc.
// SPDX-License-Identifier: ISC

using PSql.Deploy.Utilities;

namespace PSql.Deploy.Commands;

/// <summary>
///   Base class for PSql cmdlets that contain asynchronous code.
/// </summary>
public abstract class AsyncCmdlet : Cmdlet, IConsole, IDisposable
{
    private readonly MainThreadDispatcher        _dispatcher;
    private readonly DispatchedConsole           _console;
    private readonly CancellationTokenSource     _cancellation;
    private readonly ConsoleCancellationListener _listener;

    /// <summary>
    ///   Initializes a new <see cref="AsyncCmdlet"/> instance.
    /// </summary>
    protected AsyncCmdlet()
    {
        _dispatcher   = new MainThreadDispatcher();
        _console      = new DispatchedConsole(this, _dispatcher);
        _cancellation = new CancellationTokenSource();
        _listener     = new ConsoleCancellationListener(_console, _cancellation);
    }

    /// <summary>
    ///   Gets a dispatcher that forwards invocations to the main thread.
    /// </summary>
    protected IDispatcher Dispatcher => _dispatcher;

    /// <summary>
    ///   Gets a console that forwards its invocations to the main thread.
    /// </summary>
    protected IConsole Console => _console;

    /// <summary>
    ///   Executes the command for one input record.
    /// </summary>
    protected sealed override void ProcessRecord()
    {
        var task = Task.Run(ProcessRecordAndEndAsync);

        _dispatcher.Run();

        task.GetAwaiter().GetResult();
    }

    private async Task ProcessRecordAndEndAsync()
    {
        try
        {
            await ProcessRecordAsync(_cancellation.Token);
        }
        finally
        {
            _dispatcher.End();
        }
    }

    /// <summary>
    ///   Executes the command for one input record asynchronously.
    /// </summary>
    /// <param name="cancellation">
    ///   The token to monitor for cancellation requests.
    /// </param>
    /// <returns>
    ///   A task representing the asynchronous operation.
    /// </returns>
    protected abstract Task ProcessRecordAsync(CancellationToken cancellation);

    /// <inheritdoc/>
    public void Dispose()
    {
        Dispose(managed: true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    ///   Releases resources owned by the current instance.
    /// </summary>
    /// <param name="managed">
    ///   <see langword="true"/>  to dispose managed and unmanaged resources;
    ///   <see langword="false"/> to dispose unmanaged resources only.
    /// </param>
    protected virtual void Dispose(bool managed)
    {
        if (!managed)
            return;

        _listener    .Dispose();
        _cancellation.Dispose();
        _dispatcher  .Dispose();
    }
}
