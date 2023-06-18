// Copyright 2023 Subatomix Research Inc.
// SPDX-License-Identifier: ISC

namespace PSql.Deploy;

internal static class StringExtensions
{
    internal static string? NullIfSpace(this string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value;
}
