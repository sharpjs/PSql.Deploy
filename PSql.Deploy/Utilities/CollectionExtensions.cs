// Copyright Subatomix Research Inc.
// SPDX-License-Identifier: MIT

namespace PSql.Deploy;

internal static class CollectionExtensions
{
#if CONVERTED
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
#endif

    internal static ImmutableArray<T> SelectImmutable<T>(
        this ICollection collection,
        Func<object, T>  selector)
    {
        if (collection is null)
            throw new ArgumentNullException(nameof(collection));
        if (selector is null)
            throw new ArgumentNullException(nameof(selector));

        var array = ImmutableArray.CreateBuilder<T>(collection.Count);

        foreach (var item in collection)
            array.Add(selector(item));

        return array.MoveToImmutable();
    }

    internal static ImmutableArray<TOut> SelectImmutable<TIn, TOut>(
        this IReadOnlyCollection<TIn> collection,
        Func<TIn, TOut>               selector)
    {
        if (collection is null)
            throw new ArgumentNullException(nameof(collection));
        if (selector is null)
            throw new ArgumentNullException(nameof(selector));

        var array = ImmutableArray.CreateBuilder<TOut>(collection.Count);

        foreach (var item in collection)
            array.Add(selector(item));

        return array.MoveToImmutable();
    }
}
