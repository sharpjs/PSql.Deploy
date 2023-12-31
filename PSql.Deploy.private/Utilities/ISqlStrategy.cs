// Copyright 2023 Subatomix Research Inc.
// SPDX-License-Identifier: ISC

namespace PSql.Deploy;

/// <summary>
///   A strategy to connect and execute commands upon a database.
/// </summary>
internal interface ISqlStrategy
{
    /// <summary>
    ///   Opens a connection asynchronously as determined by the property
    ///   values of the specified context, logging server messages with the
    ///   specified logger.
    /// </summary>
    /// <param name="context">
    ///   An object specifying how to connect to the database.
    /// </param>
    /// <param name="logger">
    ///   The object to use to log server messages received over the
    ///   connection.
    /// </param>
    /// <param name="cancellation">
    ///   The token to monitor for cancellation requests.
    /// </param>
    /// <returns>
    ///   A <see cref="Task"/> representing the asynchronous operation.  When
    ///   the task completes, its <see cref="Task{TResult}.Result"/> property
    ///   is set to an object representing the open connection.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    ///   <paramref name="context"/> and/or
    ///   <paramref name="logger"/> is <see langword="null"/>.
    /// </exception>
    Task<ISqlConnection> ConnectAsync(
        SqlContext        context,
        ISqlMessageLogger logger,
        CancellationToken cancellation
    );

    /// <summary>
    ///   Executes the specified command asynchronously.
    /// </summary>
    /// <param name="command">
    ///   The command to execute.
    /// </param>
    /// <param name="cancellation">
    ///   The token to monitor for cancellation requests.
    /// </param>
    /// <returns>
    ///   A <see cref="Task"/> representing the asynchronous operation.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    ///   <paramref name="command"/> is <see langword="null"/>.
    /// </exception>
    /// <exception cref="DataException">
    ///   An error was encountered during command execution.
    /// </exception>
    Task ExecuteNonQueryAsync(
        ISqlCommand       command,
        CancellationToken cancellation
    );
}
