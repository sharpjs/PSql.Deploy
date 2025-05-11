// Copyright Subatomix Research Inc.
// SPDX-License-Identifier: MIT

namespace PSql.Deploy;

/// <summary>
///   A implementation of <see cref="ITargetConnection"/> that does nothing.
/// </summary>
internal class NullTargetConnection : ITargetConnection
{
    public NullTargetConnection(Target target)
    {
        if (target is null)
            throw new ArgumentNullException(nameof(target));

        Target = target;
    }

    /// <inheritdoc/>
    public Target Target { get; }

    public Task OpenAsync(CancellationToken cancellation)
    {
        return Task.CompletedTask;
    }

    public Task ExecuteAsync(string sql, CancellationToken cancellation)
    {
        return Task.CompletedTask;
    }

    public Task ExecuteAsync<T>(string sql, Action<IDataRecord, T> consumer, T state, CancellationToken cancellation)
    {
        return Task.CompletedTask;
    }

    public void Dispose()
    {
        // NOP
    }

    public ValueTask DisposeAsync()
    {
        return ValueTask.CompletedTask;
    }
}
