// Copyright Subatomix Research Inc.
// SPDX-License-Identifier: MIT

namespace PSql.Deploy.Migrations;

/// <summary>
///   A comparer that determines the deployment order of migrations.
/// </summary>
/// <remarks>
///   This comparer orders migrations by <see cref="Migration.Name"/> using
///   case-insensitive ordinal comparison, with the special pseudo-migrations
///   <c>_Begin</c> and <c>_End</c> at the beginning and end, respectively.
/// </remarks>
public sealed class MigrationComparer : IComparer<Migration>, IComparer<string>
{
    /// <summary>
    ///   Gets the singleton instance of <see cref="MigrationComparer"/>.
    /// </summary>
    public static MigrationComparer Instance { get; }
        = new MigrationComparer();

    /// <summary>
    ///   Gets the comparer to use for migration names.
    /// </summary>
    /// <remarks>
    ///   This property returns a case-insensitive ordinal comparer.
    /// </remarks>
    internal static StringComparer NameComparer
        => StringComparer.OrdinalIgnoreCase;

    /// <inheritdoc/>
    public int Compare(Migration? x, Migration? y)
    {
        if (ReferenceEquals(x, y))
            return 0;

        if (x is null)
            return -1;

        if (y is null)
            return +1;

        return CompareCore(x.Name, y.Name);
    }

    /// <inheritdoc/>
    public int Compare(string? x, string? y)
    {
        if (ReferenceEquals(x, y))
            return 0;

        if (x is null)
            return -1;

        if (y is null)
            return +1;

        return CompareCore(x, y);
    }

    private static int CompareCore(string x, string y)
    {
        var result = GetRank(x) - GetRank(y);

        return result != 0
            ? result
            : NameComparer.Compare(x, y);
    }

    /// <summary>
    ///   Gets the rank of the specified migration name for ordering purposes.
    /// </summary>
    /// <param name="name">
    ///   A migration name.
    /// </param>
    /// <returns>
    ///   <c>-1</c> if <paramref name="name"/> is <c>"_Begin"</c>;
    ///   <c>+1</c> if <paramref name="name"/> is <c>"_End"</c>;
    ///   <c>0</c> otherwise.
    /// </returns>
    /// <remarks>
    ///   This method uses case-insensitive ordinal comparison.
    /// </remarks>
    internal static int GetRank(string name)
    {
        if (NameComparer.Equals(name, Migration.BeginPseudoMigrationName))
            return -1;

        if (NameComparer.Equals(name, Migration.EndPseudoMigrationName))
            return +1;

        return 0;
    }

    /// <summary>
    ///   Gets whether the specified migration name denotes a <c>_Begin</c> or
    ///   <c>_End</c> pseudo-migration.
    /// </summary>
    /// <param name="name">
    ///   A migration name.
    /// </param>
    /// <returns>
    ///   <see langword="true"/> if <paramref name="name"/>
    ///     is <c>"_Begin"</c> or <c>"_End"</c>;
    ///   <see langword="false"/> otherwise.
    /// </returns>
    /// <remarks>
    ///   This method uses case-insensitive ordinal comparison.
    /// </remarks>
    internal static bool IsPseudo(string name)
        => GetRank(name) is not 0;
}
