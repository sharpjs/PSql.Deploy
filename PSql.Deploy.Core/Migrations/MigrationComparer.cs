// Copyright 2023 Subatomix Research Inc.
// SPDX-License-Identifier: ISC

namespace PSql.Deploy.Migrations;

/// <summary>
///   A comparer that determines the deployment order of migrations.
/// </summary>
/// <remarks>
///   This comparer orders migrations by <see cref="Migration.Name"/> using
///   case-insensitive ordinal comparison, with the special pseudo-migrations
///   <c>_Begin</c> and <c>_End</c> at the beginning and end, respectively.
/// </remarks>
public sealed class MigrationComparer : IComparer<Migration>
{
    /// <summary>
    ///   Gets the singleton instance of <see cref="MigrationComparer"/>.
    /// </summary>
    public static MigrationComparer
        Instance = new MigrationComparer();

    private static StringComparer
        NameComparer => StringComparer.OrdinalIgnoreCase;

    /// <inheritdoc/>
    public int Compare(Migration? x, Migration? y)
    {
        if (ReferenceEquals(x, y))
            return 0;

        if (x is null)
            return -1;

        if (y is null)
            return +1;

        var result = GetRank(x.Name) - GetRank(y.Name);

        return result != 0
            ? result
            : NameComparer.Compare(x.Name, y.Name);
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
}
