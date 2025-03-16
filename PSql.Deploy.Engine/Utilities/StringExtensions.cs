// Copyright Subatomix Research Inc.
// SPDX-License-Identifier: MIT

namespace PSql.Deploy;

internal static class StringExtensions
{
    public static string? NullIfEmpty(this string? s)
        => s is null or "" ? null : s;
}
