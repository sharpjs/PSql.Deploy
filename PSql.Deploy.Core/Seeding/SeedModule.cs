// Copyright 2023 Subatomix Research Inc.
// SPDX-License-Identifier: ISC

namespace PSql.Deploy.Seeding;

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
    public SeedModule(
        string                 name,
        ImmutableArray<string> batches,
        ImmutableArray<string> provides,
        ImmutableArray<string> requires)
    {
        if (name is null)
            throw new ArgumentNullException(nameof(name));

        Name     = name;
        Batches  = batches;
        Provides = provides;
        Requires = requires;
    }

    /// <summary>
    ///   Gets the name of the module.
    /// </summary>
    public string Name { get; }

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
