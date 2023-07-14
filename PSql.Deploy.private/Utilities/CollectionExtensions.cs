// Copyright 2023 Subatomix Research Inc.
// SPDX-License-Identifier: ISC

namespace PSql.Deploy;

internal static class CollectionExtensions
{
    public static ImmutableArray<T> Build<T>(this ImmutableArray<T>.Builder builder)
    {
        if (builder is null)
            throw new ArgumentNullException(nameof(builder));

        return builder.Count == builder.Capacity
            ? builder.MoveToImmutable()
            : builder.    ToImmutable();
    }
}
