// Copyright Subatomix Research Inc.
// SPDX-License-Identifier: MIT

namespace PSql.Deploy;

/// <summary>
///   A connection to a target database.
/// </summary>
internal interface ITargetConnection : IDisposable, IAsyncDisposable
{
    /// <summary>
    ///   Gets the target database of the connection.
    /// </summary>
    Target Target { get; }

    /// <summary>
    ///   Opens the connection to the target database.
    /// </summary>
    /// <param name="cancellation">
    ///   A token to monitor for cancellation requests.
    /// </param>
    /// <returns>
    ///   A <see cref="Task"/> representing the asynchronous operation.
    /// </returns>
    Task OpenAsync(
        CancellationToken cancellation
    );

    /// <summary>
    ///   Excutes the specified command asynchronously against the target
    ///   database.
    /// </summary>
    /// <param name="sql">
    ///   The command to execute against the target database.
    /// </param>
    /// <param name="cancellation">
    ///   A token to monitor for cancellation requests.
    /// </param>
    /// <returns>
    ///   A <see cref="Task"/> representing the asynchronous operation.
    /// </returns>
    Task ExecuteAsync(
        string            sql,
        CancellationToken cancellation
    );

    /// <summary>
    ///   Excutes the specified command asynchronously against the target
    ///   database.
    /// </summary>
    /// <param name="sql">
    ///   The command to execute against the target database.
    /// </param>
    /// <param name="consumer">
    /// </param>
    /// <param name="state">
    /// </param>
    /// <param name="cancellation">
    ///   A token to monitor for cancellation requests.
    /// </param>
    /// <returns>
    ///   A <see cref="Task"/> representing the asynchronous operation.
    /// </returns>
    Task ExecuteAsync<T>(
        string                 sql,
        Action<IDataRecord, T> consumer,
        T                      state,
        CancellationToken      cancellation
    );
}
