// Copyright 2023 Subatomix Research Inc.
// SPDX-License-Identifier: ISC

namespace PSql.Deploy.Utilities;

/// <summary>
///   A cmdlet that can use <see cref="AsyncCmdletScope"/>.
/// </summary>
internal interface IAsyncCmdlet : IConsole
{
    /// <summary>
    ///   Gets or sets the command runtime.
    /// </summary>
    /// <remarks>
    ///   The command runtime is responsible for console writes, host access,
    ///   and transaction access.
    /// </remarks>
    ICommandRuntime CommandRuntime { get; set; }
}
