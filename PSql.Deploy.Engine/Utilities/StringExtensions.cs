// Copyright Subatomix Research Inc.
// SPDX-License-Identifier: MIT

using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace PSql.Deploy;

/// <summary>
///   Extension methods for <see langword="string"/>.
/// </summary>
internal static class StringExtensions
{
    /// <summary>
    ///   Checks whether the string is either <see langword="null"/> or empty.
    /// </summary>
    /// <param name="s">
    ///   The string to check.
    /// </param>
    /// <returns>
    ///   <see langword="true"/> if <paramref name="s"/> is either
    ///     <see langword="null"/> or empty;
    ///   <see langword="false"/> otherwise.
    /// </returns>
    public static bool IsNullOrEmpty([NotNullWhen(false)] this string? s)
        => string.IsNullOrEmpty(s);

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

    /// <summary>
    ///   Escapes the string for inclusing in a SQL string literal.
    /// </summary>
    /// <param name="s">
    ///   The string to escape.
    /// </param>
    /// <returns>
    ///   <paramref name="s"/> escaped for use in a SQL string literal.
    /// </returns>
    public static string EscapeForSqlString(this string s)
        => s.Replace("'", "''");
}
