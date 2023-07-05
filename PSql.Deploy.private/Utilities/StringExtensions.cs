// Copyright 2023 Subatomix Research Inc.
// SPDX-License-Identifier: ISC

using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace PSql.Deploy;

internal static class StringExtensions
{
    [return: NotNullIfNotNull(nameof(value))]
    internal static string? SanitizeFileName(this string? value)
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
