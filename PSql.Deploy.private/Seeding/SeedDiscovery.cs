// Copyright 2023 Subatomix Research Inc.
// SPDX-License-Identifier: ISC

namespace PSql.Deploy.Seeding;

internal static class SeedDiscovery
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
            throw new ArgumentNullException("names[i]");

        path = Path.Combine(path, name, "_Main.sql");

        if (!File.Exists(path))
            throw new FileNotFoundException(null, path);

        return new Seed(name, path);
    }
}
