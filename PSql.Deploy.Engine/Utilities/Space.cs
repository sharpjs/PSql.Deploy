// Copyright Subatomix Research Inc.
// SPDX-License-Identifier: MIT

#pragma warning disable IDE1006 // Naming Styles

namespace PSql.Deploy;

/// <summary>
///   Spacing utilities.
/// </summary>
internal static class Space
{
    private static string?[]       _spaces = new string?[8];
    private static readonly object _lock   = new();

    /// <summary>
    ///   Gets a spacing string that would pad the specified string to the
    ///   specified width.
    /// </summary>
    /// <param name="s">
    ///   The string which the result should pad to <paramref name="width"/>.
    /// </param>
    /// <param name="width">
    ///   The width to which to the result should pad <paramref name="s"/>.
    /// </param>
    /// <returns>
    ///   A string of spaces that would yield a string of length
    ///   <paramref name="width"/> if concatenated with <paramref name="s"/>.
    ///   If <c>s</c> is longer than <c>width</c>, this method returns an empty
    ///   string.
    /// </returns>
    public static string Pad(string s, int width)
    {
        return Get(width - s.Length);
    }

    /// <summary>
    ///   Gets a pair of spacing strings that would center the specified string
    ///   in a field of the specified width.
    /// </summary>
    /// <param name="s">
    ///   The string which the result should center in a field of
    ///   <paramref name="width"/>.
    /// </param>
    /// <param name="width">
    ///   The width of the field in which the result should center
    ///   <paramref name="s"/>.
    /// </param>
    /// <returns>
    ///   A pair of strings-of-spaces of approximately equal length that would
    ///   yield a string of length <paramref name="width"/> if concatenated
    ///   with <paramref name="s"/>.  If <c>s</c> is longer than <c>width</c>,
    ///   this method returns a pair of empty strings.
    /// </returns>
    public static (string Left, string Right) Center(string s, int width)
    {
        return GetCentering(width - s.Length);
    }

    /// <summary>
    ///   Gets a pair of approximately equal spacing strings with the specified
    ///   total length.
    /// </summary>
    /// <param name="n">
    ///   The total length of the spacing string pair to get.
    /// </param>
    /// <returns>
    ///   A pair of strings of approximately equal length that would yield a
    ///   string of <paramref name="n"/> spaces if concatenated.  If <c>n</c>
    ///   is less than <c>1</c>, this method returns a pair of empty strings.
    /// </returns>
    internal static (string Left, string Right) GetCentering(int n)
    {
        var half = n >> 1;
        return (Get(n - half), Get(half));
    }

    /// <summary>
    ///   Gets spacing string of the specified length.
    /// </summary>
    /// <param name="n">
    ///   The length of the spacing string to get.
    /// </param>
    /// <returns>
    ///   A string of <paramref name="n"/> spaces.  If <c>n</c> is less than
    ///   <c>1</c>, this method returns an empty string.
    /// </returns>
    internal static string Get(int n)
    {
        if (n <= 0)
            return string.Empty;

        var index = n - 1;

        if (index < _spaces.Length && _spaces[index] is { } space)
            return space;

        lock (_lock)
        {
            if (index >= _spaces.Length)
                Array.Resize(ref _spaces, MathEx.RoundUpToPowerOf2Saturating(n));

            return _spaces[index] ??= new(' ', n);
        }
    }
}
