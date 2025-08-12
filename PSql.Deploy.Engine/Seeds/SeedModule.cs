// Copyright Subatomix Research Inc.
// SPDX-License-Identifier: MIT

namespace PSql.Deploy.Seeds;

/// <summary>
///   A module in a database content seed.
/// </summary>
public class SeedModule
{
    /// <summary>
    ///   Initializes a new <see cref="SeedModule"/> instance.
    /// </summary>
    /// <param name="name">
    ///   The name of the module.
    /// </param>
    /// <param name="workerId">
    ///   A value that determines which worker threads can apply the module.
    ///   A positive value nominates a specific worker by that worker's ordinal
    ///   ID.  The default value, <c>0</c>, indicates that any worker can apply
    ///   the module.  The special value <c>-1</c> indicates that <b>every</b>
    ///   worker must apply the module.
    /// </param>
    /// <param name="batches">
    ///   The SQL batches to be executed when the module runs.
    /// </param>
    /// <param name="provides">
    ///   The names of modules that are not complete until the current module
    ///   runs to completion.
    /// </param>
    /// <param name="requires">
    ///   The names of modules that must run to completion before the current
    ///   module runs.
    /// </param>
    /// <exception cref="ArgumentNullException">
    ///   <paramref name="name"/> is <see langword="null"/>.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    ///   <paramref name="workerId"/> is less than <c>-1</c>.
    /// </exception>
    public SeedModule(
        string                 name,
        int                    workerId,
        ImmutableArray<string> batches,
        ImmutableArray<string> provides,
        ImmutableArray<string> requires)
    {
        if (name is null)
            throw new ArgumentNullException(nameof(name));
        if (workerId < -1)
            throw new ArgumentOutOfRangeException(nameof(workerId));

        Name     = name;
        WorkerId = workerId;
        Batches  = batches;
        Provides = provides;
        Requires = requires;
    }

    /// <summary>
    ///   Gets the name of the module.
    /// </summary>
    public string Name { get; }

    /// <summary>
    ///   Gets a value that determines which worker threads can apply the
    ///   module.  A positive value nominates a specific worker by that
    ///   worker's ordinal ID.  The default value, <c>0</c>, indicates that any
    ///   worker can apply the module.  The special value <c>-1</c> indicates
    ///   that <b>every</b> worker must apply the module.
    /// </summary>
    public int WorkerId { get; }

    /// <summary>
    ///   Gets the SQL batches to be executed when the module runs.
    /// </summary>
    public ImmutableArray<string> Batches { get; }

    /// <summary>
    ///   Gets the names of modules that are not complete until the current
    ///   module runs to completion.
    /// </summary>
    public ImmutableArray<string> Provides { get; }

    /// <summary>
    ///   Gets the names of modules that must run to completion before the
    ///   current module runs.
    /// </summary>
    public ImmutableArray<string> Requires { get; }
}
