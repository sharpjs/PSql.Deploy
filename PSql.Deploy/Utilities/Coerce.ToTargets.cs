// Copyright Subatomix Research Inc.
// SPDX-License-Identifier: MIT

using System.Collections;

namespace PSql.Deploy;

partial class Coerce // -> IReadOnlyList<Target>
{
    internal static IReadOnlyList<Target> ToTargetListRequired(object? obj)
    {
        return ToTargetList(obj) ?? throw OnFailure(obj, "one or more target databases");
    }

    private static IReadOnlyList<Target>? ToTargetList(object? obj)
    {
        if (obj is null)
            return null;

        return obj is PSObject psObject
            ? ToTargetListSpecialCase(psObject.BaseObject) ?? ToTargetListFromOther(psObject)
            : ToTargetListSpecialCase(obj)                 ?? ToTargetListFromOther(obj);
    }

    private static IReadOnlyList<Target>? ToTargetListSpecialCase(object obj)
    {
        if (obj is IReadOnlyList<Target> list)
            return list;

        if (obj is IEnumerable enumerable)
            return ToTargetListFromEnumerable(enumerable);

        return null;
    }

    private static IReadOnlyList<Target>? ToTargetListFromEnumerable(IEnumerable enumerable)
    {
        var array = enumerable is ICollection collection
            ? ImmutableArray.CreateBuilder<Target>(collection.Count)
            : ImmutableArray.CreateBuilder<Target>();

        foreach (var item in enumerable)
            array.Add(ToTargetRequired(item));

        return array.MoveToImmutable();
    }

    private static IReadOnlyList<Target>? ToTargetListFromOther(object obj)
    {
        if (ToTarget(obj) is { } target)
            return ImmutableArray.Create(target);

        return null;
    }
}
