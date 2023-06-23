// Copyright 2023 Subatomix Research Inc.
// SPDX-License-Identifier: ISC

using System.Collections.Concurrent;

namespace PSql.Deploy.Migrations;

internal class ThreadSafeMigrationLogger : IMigrationLogger
{
    private readonly BlockingCollection<Action<IMigrationLogger>> _queue;
    private readonly IMigrationLogger                             _logger;
    private readonly int                                          _mainThreadId;

    public ThreadSafeMigrationLogger(IMigrationLogger logger)
    {
        if (logger is null)
            throw new ArgumentNullException(nameof(logger));

        _logger       = logger;
        _queue        = new();
        _mainThreadId = Thread.CurrentThread.ManagedThreadId;
    }

    public void Run(CancellationToken cancellation)
    {
        if (Thread.CurrentThread.ManagedThreadId != _mainThreadId)
            throw new InvalidOperationException(
                "MigrationLogger.Run must be invoked from the thread that " +
                "constructed the MigrationLogger."
            );

        while (_queue.TryTake(out var action, -1, cancellation))
            action(_logger);
    }

    public void Log(MigrationMessage message)
    {
        Post(logger => logger.Log(message));
    }

    public void LogInformation(string message)
    {
        Post(logger => logger.LogInformation(message));
    }

    public void LogWarning(string message)
    {
        Post(logger => logger.LogWarning(message));
    }

    public void Post(Action<IMigrationLogger> action)
    {
        if (Thread.CurrentThread.ManagedThreadId == _mainThreadId)
            action(_logger);
        else
            _queue.Add(action);
    }

    public void Stop()
    {
        _queue.CompleteAdding();
    }
}
