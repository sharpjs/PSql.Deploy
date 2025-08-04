// Copyright Subatomix Research Inc.
// SPDX-License-Identifier: MIT

using System.Collections.Concurrent;
using System.IO.MemoryMappedFiles;
using System.Security.Cryptography;

namespace PSql.Deploy.Migrations;

/// <summary>
///   Methods to discover migrations.
/// </summary>
public static class MigrationDiscoverer
{
    /// <summary>
    ///   Gets migrations defined in the filesystem at the specified path.
    /// </summary>
    /// <param name="path">
    ///   The path of a directory containing migrations.
    /// </param>
    /// <param name="maxName">
    ///   The maximum (latest) name of migrations to return, or
    ///   <see langword="null"/> to return all migrations.
    /// </param>
    /// <returns>
    ///   The migrations defined at <paramref name="path"/>, or an empty array
    ///   if no directory exists at that path.  If <paramref name="maxName"/>
    ///   is not <see langword="null"/>, the return value includes only those
    ///   migrations whose names are less than (earlier than)
    ///   <paramref name="maxName"/>.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    ///   <paramref name="path"/> is <see langword="null"/>.
    /// </exception>
    public static ImmutableArray<Migration> GetAll(string path, string? maxName = null)
    {
        if (path is null)
            throw new ArgumentNullException(nameof(path));

        if (!Directory.Exists(path))
            return ImmutableArray<Migration>.Empty;

        path = Path.Combine(path, "Migrations");

        if (!Directory.Exists(path))
            return ImmutableArray<Migration>.Empty;

        var bag = new ConcurrentBag<Migration>();

        Parallel.ForEach(
            EnumerateDirectories(path, maxName),
            path => DetectMigration(path, bag)
        );

        return Sort(bag);
    }

    private static IEnumerable<string> EnumerateDirectories(string path, string? maxName)
    {
        var paths = Directory.EnumerateDirectories(path);

        if (maxName is not null)
            paths = paths.Where(p => HasNameLessThanOrEqualTo(p, maxName));

        return paths;
    }

    private static bool HasNameLessThanOrEqualTo(string path, string maxName)
    {
        var name = Path.GetFileName(path);

        return MigrationComparer.IsPseudo(name) // always discover _Begin and _End
            || MigrationComparer.NameComparer.Compare(name, maxName) <= 0;
    }

    private static void DetectMigration(string path, ConcurrentBag<Migration> bag)
    {
        var file = new FileInfo(Path.Combine(path, "_Main.sql"));
        if (file.Exists)
            bag.Add(MakeMigration(file));
    }

    private static Migration MakeMigration(FileInfo file)
    {
        // File is known to have a parent directory
        var directory = file.Directory!;

        return new Migration(directory.Name)
        {
            Path = file.FullName,
            Hash = GetMigrationHash(directory),
        };
    }

    private static string GetMigrationHash(DirectoryInfo directory)
    {
        using var hashes = new MemoryStream();

        // Hash each SQL file in the migration
        foreach (var file in FindSqlFiles(directory))
            hashes.Write(GetHash(file));

        // Hash the hashes
        hashes.Position = 0;
        return Convert.ToHexString(GetHash(hashes));
    }

    private static FileInfo[] FindSqlFiles(DirectoryInfo directory)
    {
        var files = directory.GetFiles("*.sql", new EnumerationOptions
        {
            IgnoreInaccessible    = false,
            RecurseSubdirectories = true,
        });

        // Specifically for hash computation, sort files by full path, using
        // ordinal case-sensitive comparison.  This ensures hash stability
        // across different file systems, OSes, and case folding rules.
        static int Compare(FileInfo x, FileInfo y)
            => StringComparer.Ordinal.Compare(x.FullName, y.FullName);

        Array.Sort(files, Compare);

        return files;
    }

    private static ReadOnlySpan<byte> GetHash(FileInfo file)
    {
        using var memory = MapToMemory(file);
        using var stream = GetStream(file, memory);

        return GetHash(stream);
    }

    private static ReadOnlySpan<byte> GetHash(Stream stream)
    {
        // Assume SHA1 is a supported hash algorithm
        using var algorithm = HashAlgorithm.Create("SHA1")!;

        return algorithm.ComputeHash(stream);
    }

    private static MemoryMappedFile MapToMemory(FileInfo file)
    {
        return MemoryMappedFile.CreateFromFile(
            file.FullName,
            FileMode.Open,
            mapName:  null, // do not share across processes
            capacity: 0,    // use capacity same as file size
            MemoryMappedFileAccess.Read
        );
    }

    private static MemoryMappedViewStream GetStream(FileInfo file, MemoryMappedFile memory)
    {
        return memory.CreateViewStream(0, file.Length, MemoryMappedFileAccess.Read);
    }

    private static ImmutableArray<Migration> Sort(ConcurrentBag<Migration> bag)
    {
        var array = ImmutableArray.CreateBuilder<Migration>(bag.Count);

        array.AddRange(bag);
        array.Sort(MigrationComparer.Instance);

        return array.MoveToImmutable();
    }
}
