// Copyright 2023 Subatomix Research Inc.
// SPDX-License-Identifier: ISC

namespace PSql.Deploy.Utilities;

/// <summary>
///   Context provided to the asynchronous code invoked by
///   <see cref="AsyncCmdletScope"/>.
/// </summary>
internal interface IAsyncCmdletContext
{
    /// <summary>
    ///   Gets a dispatcher that forwards invocations to the main thread.
    /// </summary>
    IDispatcher Dispatcher { get; }

    /// <summary>
    ///   Gets the token to monitor for cancellation requests.
    /// </summary>
    CancellationToken CancellationToken { get; }
}
