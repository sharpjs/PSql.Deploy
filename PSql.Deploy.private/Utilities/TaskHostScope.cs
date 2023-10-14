#if ADJUST_FOR_TASKHOST_2
// Copyright 2023 Subatomix Research Inc.
// SPDX-License-Identifier: ISC

using System.Management.Automation.Host;
using Subatomix.PowerShell.TaskHost;

namespace PSql.Deploy.Utilities;

/// <summary>
///   A wrapper for <see cref="PSHost"/> to improve the clarity of output from
///   parallel tasks.
/// </summary>
public sealed class TaskHostScope : IDisposable
{
    private readonly TaskHost _host;

    internal TaskHostScope(TaskHost taskHost)
        => _host = taskHost;

    /// <summary>
    ///   Gets the host wrapper.
    /// </summary>
    public PSHost Host
        => _host;

    /// <inheritdoc/>
    public void Dispose()
        => _host.Dispose();
}
#endif
