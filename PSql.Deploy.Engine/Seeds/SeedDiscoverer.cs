// Copyright Subatomix Research Inc.
// SPDX-License-Identifier: MIT

namespace PSql.Deploy.Seeds;

internal class SeedDiscoverer
{
    internal static ImmutableArray<Seed> Get(string path, string[] names)
    {
        if (path is null)
            throw new ArgumentNullException(nameof(path));
        if (names is null)
            throw new ArgumentNullException(nameof(names));

        var builder = ImmutableArray.CreateBuilder<Seed>(names.Length);

        foreach (var name in names)
            builder.Add(DetectSeed(path, name));

        return builder.MoveToImmutable();
    }

    private static Seed DetectSeed(string path, string name)
    {
        // path null-checked by caller
        if (name is null)
            throw new ArgumentException("Argument cannot have a null element.", "names");

        path = Path.Combine(path, "Seeds", name, "_Main.sql");

        if (!File.Exists(path))
            throw new FileNotFoundException(null, path);

        return new Seed(name, path);
    }
}
