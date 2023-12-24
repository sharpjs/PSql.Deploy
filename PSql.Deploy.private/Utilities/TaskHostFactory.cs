#if REWORK
// Copyright 2023 Subatomix Research Inc.
// SPDX-License-Identifier: ISC

using System.Management.Automation.Host;
using Inner = Subatomix.PowerShell.TaskHost;

namespace PSql.Deploy.Utilities;

/// <summary>
///   A factory to create <see cref="TaskHostScope"/> instances.
/// </summary>
public sealed class TaskHostFactory
{
    private readonly Inner.TaskHostFactory _factory;

    public TaskHostFactory(PSHost host)
        => _factory = new Inner.TaskHostFactory(host, withElapsed: true);

    /// <summary>
    ///   Creates a new <see cref="TaskHostScope"/> instance.
    /// </summary>
    /// <param name="name">
    ///   The name of the instance.
    /// </param>
    public TaskHostScope BeginScope(string name)
        => new(_factory.Create(name));
}
#endif
