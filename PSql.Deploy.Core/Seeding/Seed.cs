// Copyright 2023 Subatomix Research Inc.
// SPDX-License-Identifier: ISC

namespace PSql.Deploy.Seeding;

/// <summary>
///   A database content seed.
/// </summary>
public class Seed
{
    /// <summary>
    ///   Initializes a new <see cref="Seed"/> instance.
    /// </summary>
    /// <param name="name">
    ///   The name of the seed.
    /// </param>
    /// <param name="path">
    ///   The full path of the <c>_Main.sql</c> file of the seed.
    /// </param>
    /// <exception cref="ArgumentNullException">
    ///   <paramref name="name"/> and/or <paramref name="path"/> is
    ///   <see langword="null"/>.
    /// </exception>
    public Seed(string name, string path)
    {
        if (name is null)
            throw new ArgumentNullException(nameof(name));
        if (path is null)
            throw new ArgumentNullException(nameof(path));

        Name = name;
        Path = path;
    }

    /// <summary>
    ///   Gets the name of the seed.
    /// </summary>
    public string Name { get; }

    /// <summary>
    ///   Gets the full path <c>_Main.sql</c> file of the seed.
    /// </summary>
    public string Path { get; }
}
