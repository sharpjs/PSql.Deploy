// Copyright 2023 Subatomix Research Inc.
// SPDX-License-Identifier: ISC

using System.Collections.Concurrent;

namespace PSql.Deploy;

/// <summary>
///   A dispatcher that executes dispatched actions on a main thread.
/// </summary>
internal sealed class MainThreadDispatcher : IDispatcher, IDisposable
{
    private readonly BlockingCollection<Action> _queue;
    private readonly int                        _mainThreadId;

    /// <summary>
    ///   Initializes a new <see cref="MainThreadDispatcher"/> instance,
    ///   nominating the current thread as the main thread.
    /// </summary>
    public MainThreadDispatcher()
    {
        _queue        = new();
        _mainThreadId = CurrentThreadId;
    }

    private int CurrentThreadId => Thread.CurrentThread.ManagedThreadId;

    /// <summary>
    ///   Executes dispatched actions until <see cref="End"/> is invoked.
    /// </summary>
    /// <remarks>
    ///   This method must be invoked from the main thread (the thread that
    ///   constructed the dispatcher instance).
    /// </remarks>
    /// <exception cref="IOException">
    ///   This method was invoked from a thread other than the thread that
    ///   constructed the dispatcher instance.
    /// </exception>
    public void Run()
    {
        if (CurrentThreadId != _mainThreadId)
            throw OnInvokedFromOtherThread();

        while (_queue.TryTake(out var action, -1 /* indefinitely */))
            action();
    }

    /// <summary>
    ///   Dispatches the specified action to execute on the main thread.
    /// </summary>
    /// <param name="action">
    ///   The action to dispatch.
    /// </param>
    /// <remarks>
    ///   If invoked from the main thread (the thread that constructed the
    ///   dispatcher), this method executes the <paramref name="action"/>
    ///   immediately and synchronously.  Otherwise, this method queues the
    ///   action for deferred execution by the <see cref="Run"/> loop on the
    ///   main thread.
    /// </remarks>
    public void Post(Action action)
    {
        if (CurrentThreadId == _mainThreadId)
            action();
        else
            _queue.Add(action);
    }

    /// <summary>
    ///   Indicates that there are no further actions to dispatch.  The
    ///   <see cref="Run"/> loop will return after executing any remaining
    ///   deferred actions.
    /// </summary>
    public void End()
    {
        _queue.CompleteAdding();
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        _queue.Dispose();
    }

    private static Exception OnInvokedFromOtherThread()
    {
        return new InvalidOperationException(
            "This method must be invoked from the thread that constructed the dispatcher."
        );
    }
}
