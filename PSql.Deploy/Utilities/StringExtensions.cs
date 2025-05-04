// Copyright Subatomix Research Inc.
// SPDX-License-Identifier: MIT

using System.Text;

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

    [return: NotNullIfNotNull(nameof(value))]
    public static string? SanitizeFileName(this string? value)
    {
        if (value is null)
            return null;

        var invalid = Path.GetInvalidFileNameChars();
        var index   = value.IndexOfAny(invalid);
        if (index < 0)
            return value;

        var start  = 0;
        var result = new StringBuilder(value.Length);

        do
        {
            result.Append(value, start, index - start).Append('_');

            start = index + 1;
            index = value.IndexOfAny(invalid, start);
        }
        while (index >= 0);

        return result.Append(value, start, value.Length - start).ToString();
    }
}
