// Copyright 2023 Subatomix Research Inc.
// SPDX-License-Identifier: ISC

namespace PSql.Deploy.Seeding;

/// <summary>
///   A loaded database content seed.
/// </summary>
public class LoadedSeed
{
    /// <summary>
    ///   Initializes a new <see cref="LoadedSeed"/> instance.
    /// </summary>
    /// <param name="seed">
    ///   The seed that has been loaded.
    /// </param>
    /// <param name="modules">
    ///   The modules defined in <paramref name="seed"/>.
    /// </param>
    /// <exception cref="ArgumentNullException">
    ///   <paramref name="seed"/> is <see langword="null"/>.
    /// </exception>
    public LoadedSeed(Seed seed, ImmutableArray<SeedModule> modules)
    {
        if (seed is null)
            throw new ArgumentNullException(nameof(seed));

        Seed    = seed;
        Modules = modules;
    }

    /// <summary>
    ///   Gets the seed that has been loaded.
    /// </summary>
    public Seed Seed { get; }

    /// <summary>
    ///   Gets the modules defined in the seed.
    /// </summary>
    public ImmutableArray<SeedModule> Modules { get; }
}
