// Copyright 2023 Subatomix Research Inc.
// SPDX-License-Identifier: ISC

using DependencyQueue;

namespace PSql.Deploy.Seeding;

using QueueEntry = DependencyQueueEntry<IEnumerable<string>>;

/// <summary>
///   A named sequence of SQL batches to be executed during a seed run.
/// </summary>
public class SeedModule
{
    internal SeedModule(QueueEntry entry)
    {
        Name    = entry.Name;
        Batches = entry.Value;
    }

    /// <summary>
    ///   Gets the name of the module.
    /// </summary>
    public string Name { get; }

    /// <summary>
    ///   Gets the sequence of SQL batches to execute.
    /// </summary>
    public IEnumerable<string> Batches { get; }
}
