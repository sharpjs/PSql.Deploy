// Copyright Subatomix Research Inc.
// SPDX-License-Identifier: MIT

namespace PSql.Deploy;

internal static class CollectionExtensions
{
    internal static T[] Sanitize<T>(this T?[]? array)
        where T : class
    {
        if (array is null)
            return [];

        var count = 0;

        foreach (var item in array)
            if (item is not null)
                count++;

        if (count == array.Length)
            return array!;

        var result = new T[count];
            count  = 0;

        foreach (var item in array)
            if (item is not null)
                result[count++] = item;

        return result;
    }
}
