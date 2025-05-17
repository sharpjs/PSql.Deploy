// Copyright Subatomix Research Inc.
// SPDX-License-Identifier: MIT

namespace PSql.Deploy;

/// <summary>
///   A connection to a target database.
/// </summary>
internal interface ITargetConnection : IDisposable, IAsyncDisposable
{
    /// <summary>
    ///   Gets an object representing the target database.
    /// </summary>
    Target Target { get; }

    /// <summary>
    ///   Gets the logger for server messages received over the connection.
    /// </summary>
    ISqlMessageLogger Logger { get; }

    /// <summary>
    ///   Opens the connection to the target database.
    /// </summary>
    /// <param name="cancellation">
    ///   A token to monitor for cancellation requests.
    /// </param>
    /// <returns>
    ///   A <see cref="Task"/> representing the asynchronous operation.
    /// </returns>
    Task OpenAsync(CancellationToken cancellation);
}
