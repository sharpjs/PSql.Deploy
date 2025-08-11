// Copyright Subatomix Research Inc.
// SPDX-License-Identifier: MIT

namespace PSql.Deploy;

/// <summary>
///   A connection to a target database for what-if simulation purposes.
/// </summary>
internal abstract class WhatIfTargetConnection : ITargetConnection
{
    private readonly ITargetConnection _connection;

    /// <summary>
    ///   Initializes a new <see cref="WhatIfTargetConnection"/> instance
    ///   wrapping the specified connection.
    /// </summary>
    /// <param name="connection">
    ///   The connection to be wrapped.
    /// </param>
    /// <exception cref="ArgumentNullException">
    ///   <paramref name="connection"/> is <see langword="null"/>.
    /// </exception>
    protected WhatIfTargetConnection(ITargetConnection connection)
    {
        if (connection is null)
            throw new ArgumentNullException(nameof(connection));

        _connection = connection;
    }

    /// <summary>
    ///   Gets the underlying connection.
    /// </summary>
    protected ITargetConnection UnderlyingConnection
        => _connection;

    /// <inheritdoc/>
    public Target Target
        => _connection.Target;

    /// <inheritdoc/>
    public ISqlMessageLogger Logger
        => _connection.Logger;

    /// <inheritdoc/>
    public virtual Task OpenAsync(CancellationToken cancellation)
        => _connection.OpenAsync(cancellation);

    /// <inheritdoc/>
    public virtual void Dispose()
        => _connection.Dispose();

    /// <inheritdoc/>
    public virtual ValueTask DisposeAsync()
        => _connection.DisposeAsync();

    /// <summary>
    ///   Logs the specified message.
    /// </summary>
    /// <param name="message">
    ///   The message to log.
    /// </param>
    protected void Log(string? message)
        => Logger.Log("", 0, 0, 0, message);
}
