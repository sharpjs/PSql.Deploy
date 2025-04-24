// Copyright Subatomix Research Inc.
// SPDX-License-Identifier: MIT

namespace PSql.Deploy;

internal static class StringExtensions
{
    // TODO: Dedupe with same in Engine

    /// <summary>
    ///   Replaces an empty string with <see langword="null"/>.
    /// </summary>
    /// <param name="s">
    ///   The string to transform.
    /// </param>
    /// <returns>
    ///   <see langword="null"/> if <paramref name="s"/> is empty;
    ///   <paramref name="s"/> otherwise.
    /// </returns>
    public static string? NullIfEmpty(this string? s)
        => string.IsNullOrEmpty(s) ? null : s;
}
