// Copyright Subatomix Research Inc.
// SPDX-License-Identifier: MIT

namespace PSql.Deploy;

internal class WhatIfTargetConnection : ITargetConnection
{
    private readonly ITargetConnection _connection;

    internal WhatIfTargetConnection(ITargetConnection connection)
    {
        if (connection is null)
            throw new ArgumentNullException(nameof(connection));

        _connection = connection;
    }

    protected ITargetConnection UnderlyingConnection
        => _connection;

    /// <inheritdoc/>
    public Target Target
        => _connection.Target;

    /// <inheritdoc/>
    public ISqlMessageLogger Logger
        => _connection.Logger;

    /// <inheritdoc/>
    public Task OpenAsync(CancellationToken cancellation)
        => _connection.OpenAsync(cancellation);

    /// <inheritdoc/>
    public virtual void Dispose()
        => _connection.Dispose();

    /// <inheritdoc/>
    public virtual ValueTask DisposeAsync()
        => _connection.DisposeAsync();

    protected void Log(string message)
        => Logger.Log("", 0, 0, 0, message);
}
