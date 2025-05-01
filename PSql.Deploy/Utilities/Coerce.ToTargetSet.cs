// Copyright Subatomix Research Inc.
// SPDX-License-Identifier: MIT

using System.Collections;

namespace PSql.Deploy;

partial class Coerce // -> TargetSet
{
    internal static TargetSet ToTargetSetRequired(object? obj)
    {
        return ToTargetSet(obj) ?? throw OnFailure(obj, "a target database set");
    }

    internal static TargetSet? ToTargetSet(object? obj)
    {
        if (obj is null)
            return null;

        return obj is PSObject psObject
            ? ToTargetSetSpecialCase(psObject.BaseObject) ?? ToTargetSetFromPSObject(psObject)
            : ToTargetSetSpecialCase(obj)                 ?? ToTargetSetFromOther(obj);
    }

    private static TargetSet? ToTargetSetSpecialCase(object obj)
    {
        if (obj is TargetSet targetSet)
            return targetSet;

        if (obj is Target target)
            return ToTargetSetFromTarget(target);

        if (obj is string conectionString)
            return ToTargetSetFromConnectionString(conectionString);

        if (obj is IDictionary dictionary)
            return ToTargetSetFromDictionary(dictionary);

        if (obj is IEnumerable enumerable)
            return ToTargetSetFromEnumerable(enumerable);

        var type = obj.GetType();
        if (type.IsSqlContextType())
            return ToTargetSetFromSqlContext(obj, type);

        return null;
    }

    private static TargetSet ToTargetSetFromTarget(Target target)
    {
        return new([target]);
    }

    private static TargetSet? ToTargetSetFromConnectionString(string conectionString)
    {
        var target = ToTargetFromConnectionString(conectionString);
        if (target is null)
            return null;

        return ToTargetSetFromTarget(target);
    }

    private static TargetSet? ToTargetSetFromDictionary(IDictionary dictionary)
    {
        return ToTargetSetFromStructure(dictionary, GetDictionaryValue);
    }

    private static TargetSet? ToTargetSetFromEnumerable(IEnumerable enumerable)
    {
        throw new NotImplementedException();
    }

    private static TargetSet? ToTargetSetFromSqlContext(object obj, Type type)
    {
        throw new NotImplementedException();
    }

    private static TargetSet? ToTargetSetFromPSObject(PSObject obj)
    {
        return ToTargetSetFromStructure(obj, GetPSPropertyValue);
    }

    private static TargetSet? ToTargetSetFromOther(object obj)
    {
        return ToTargetSetFromStructure(obj.WithType(), GetPropertyValue);
    }

    private static TargetSet? ToTargetSetFromStructure<T>(T source, Func<T, string, object?> accessor)
    {
        var targets = accessor(source, "Targets") as IReadOnlyList<Target>;
        if (targets is null)
            return null;

        var name                    = accessor(source, "Name").ToNonEmptyString();
        var maxParallelism          = accessor(source, "MaxParallelism")          as int? ?? 0;
        var maxParallelismPerTarget = accessor(source, "MaxParallelismPerTarget") as int? ?? 0;

        return new(targets, name, maxParallelism, maxParallelismPerTarget);
    }
}
