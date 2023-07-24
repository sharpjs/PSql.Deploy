// Copyright 2023 Subatomix Research Inc.
// SPDX-License-Identifier: ISC

using DependencyQueue;
using Subatomix.PowerShell.TaskHost;

namespace PSql.Deploy.Seeding;

using ContextData  = Dictionary<string, object?>;
using QueueContext = DependencyQueueContext<IEnumerable<string>, Dictionary<string, object?>>;

/// <summary>
///   Contextual information provided to a worker during a seed run.
/// </summary>
public class SeedContext
{
    private readonly QueueContext _context;
    private readonly TaskHost     _host;

    internal SeedContext(QueueContext context, TaskHost host)
    {
        _context = context;
        _host    = host;
    }

    /// <summary>
    ///   Gets the unique identifier of the seed run.
    ///   The identifier is a random GUID.
    /// </summary>
    public Guid RunId
        => _context.RunId;

    /// <summary>
    ///   Gets the unique identifier of the worker within the seed run.
    ///   The identifier is an ordinal number.
    /// </summary>
    public int WorkerId
        => _context.WorkerId;

    /// <summary>
    ///   Gets additional data provided by the seed runner.
    /// </summary>
    public ContextData Data
        => _context.Data;

    /// <summary>
    ///   Gets or sets the header that identifies worker output.
    /// </summary>
    public string Header
    {
        get => _host.TaskHostUI.Header;
        set => _host.TaskHostUI.Header = value;
    }

    /// <summary>
    ///   Gets the next module that the worker should execute.
    /// </summary>
    /// <returns>
    ///   A module to execute, or null if no more modules remain to be
    ///   executed.
    /// </returns>
    public SeedModule? GetNextModule()
    {
        var entry = _context.GetNextEntry();

        return entry is null
            ? null
            : new(entry);
    }
}
