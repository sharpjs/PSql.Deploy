// Copyright Subatomix Research Inc.
// SPDX-License-Identifier: MIT

namespace PSql.Deploy;

internal static class CollectionExtensions
{
    public static ReadOnlySpan<T> AsReadOnlySpan<T>(this T[] array)
        => array;

    public static ImmutableArray<TResult> SelectImmutable<TSource, TResult>(
        this ReadOnlySpan<TSource> source,
        Func<TSource, TResult>     selector)
    {
        if (selector is null)
            throw new ArgumentNullException(nameof(selector));

        if (source.IsEmpty)
            return ImmutableArray<TResult>.Empty;

        var builder = ImmutableArray.CreateBuilder<TResult>(source.Length);

        foreach (var item in source)
            builder.Add(selector(item));

        return builder.MoveToImmutable();
    }

    public static ImmutableArray<T> Build<T>(this ImmutableArray<T>.Builder builder)
    {
#if NET8_0_OR_GREATER
        return builder.DrainToImmutable();
#else
        if (builder is null)
            throw new ArgumentNullException(nameof(builder));

        if (builder.Count == builder.Capacity)
            return builder.MoveToImmutable();

        var array = builder.ToImmutable();
        builder.Clear();

        return array;
#endif
    }
}
