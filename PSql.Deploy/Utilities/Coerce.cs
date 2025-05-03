// Copyright Subatomix Research Inc.
// SPDX-License-Identifier: MIT

using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Reflection;

namespace PSql.Deploy;

using static BindingFlags;

/// <summary>
///   Methods for cmdlet argument coercion.
/// </summary>
internal static class Coerce
{
    #region -> TargetSetArray

    internal static TargetSet[] ToTargetSetArrayRequired(object? obj)
    {
        return ToTargetSetArray(obj) ?? throw OnFailure(obj, "one or more target database sets");
    }

    internal static TargetSet[]? ToTargetSetArray(object? obj)
    {
        if (obj is null)
            return null;

        if (obj is PSObject pSObject)
            obj = pSObject.BaseObject;

        if (obj is TargetSet[] array)
            return array;

        if (obj is ICollection collection)
            return ToTargetSetArrayFromCollection(collection);

        if (ToTargetSet(obj) is { } targetSet)
            return [targetSet];

        return null;
    }

    private static TargetSet[] ToTargetSetArrayFromCollection(ICollection collection)
    {
        var array = new TargetSet[collection.Count];
        var index = 0;

        foreach (var item in collection)
            array[index++] = ToTargetSetRequired(item);

        return array;
    }

    #endregion

    #region -> TargetSet

    internal static TargetSet ToTargetSetRequired(object? obj)
    {
        return ToTargetSet(obj) ?? throw OnFailure(obj, "a target database set");
    }

    internal static TargetSet? ToTargetSet(object? obj)
    {
        if (obj is null)
            return null;

        if (obj is PSObject pSObject)
            obj = pSObject.BaseObject;

        if (obj is TargetSet targetSet)
            return targetSet;

        if (ToTargetList(obj) is { } targetList)
            return new(targetList);

        return null;
    }

    #endregion

    #region -> TargetList

    internal static IReadOnlyList<Target> ToTargetListRequired(object? obj)
    {
        return ToTargetList(obj) ?? throw OnFailure(obj, "one or more target databases");
    }

    private static IReadOnlyList<Target>? ToTargetList(object? obj)
    {
        if (obj is null)
            return null;

        if (obj is PSObject pSObject)
            obj = pSObject.BaseObject;

        if (obj is IReadOnlyList<Target> list)
            return list;

        if (obj is ICollection collection)
            return ToTargetListFromCollection(collection);

        if (ToTarget(obj) is { } target)
            return ImmutableArray.Create(target);

        return null;
    }

    private static IReadOnlyList<Target>? ToTargetListFromCollection(ICollection collection)
    {
        var array = ImmutableArray.CreateBuilder<Target>(collection.Count);

        foreach (var item in collection)
            array.Add(ToTargetRequired(item));

        return array.MoveToImmutable();
    }
    #endregion

    #region -> Target

    internal static Target ToTargetRequired(object? obj)
    {
        return ToTarget(obj) ?? throw OnFailure(obj, "a target database");
    }

    internal static Target? ToTarget(object? obj)
    {
        if (obj == null)
            return null;

        if (obj is PSObject pSObject)
            obj = pSObject.BaseObject;

        if (obj is Target target)
            return target;

        if (obj is string text)
            return ToTargetFromConnectionString(text);

        return ToTargetFromSqlContext(obj.WithType());
    }

    private static Target? ToTargetFromConnectionString(string connectionString)
    {
        return new(connectionString);
    }

    private static Target? ToTargetFromSqlContext(ObjectTypePair source)
    {
        return IsSqlContextType(source.Type)
            && source.TryGetConnectionString  (out var connectionString)
            && source.TryGetCredential        (out var credential)
            && source.TryGetServerResourceName(out var serverDisplayName)
            ?  new(connectionString, credential, serverDisplayName)
            :  null;
    }

    private static bool IsSqlContextType(this Type type)
    {
        return type.FullName is { } name
            && name.StartsWith("PSql.",      StringComparison.Ordinal)
            && name.EndsWith  ("SqlContext", StringComparison.Ordinal);
    }

    private static bool TryGetConnectionString(
        this ObjectTypePair source, [MaybeNullWhen(false)] out string value)
    {
        const string
            VersionTypeName  = "PSql.SqlClientVersion",
            VersionValueName = "Mds5", // for Microsoft.Data.SqlClient v5
            MethodName       = "GetConnectionString";

        value = default;

        if (source.Type.Assembly.GetType(VersionTypeName) is not { IsEnum: true } versionType)
            return false;

        if (Enum.TryParse(versionType, VersionValueName, out var version))
            return false;

        var method = source.Type.GetMethod(
            name:                  MethodName,
            genericParameterCount: 0,
            bindingAttr:           Public | Instance,
            binder:                null,
            types: [
                typeof(string), // databaseName
                versionType,    // sqlClientVersion
                typeof(bool)    // omitCredential
            ],
            modifiers:             null
        );

        if (method?.ReturnType != typeof(string))
            return false;

        if (method.Invoke(source.Object, [null, version, true]) is not string s)
            return false;

        value = s;
        return true;
    }

    private static bool TryGetCredential(this ObjectTypePair source, out NetworkCredential? value)
    {
        return source.GetPropertyValue("Credential").TryCast(out value);
    }

    private static bool TryGetServerResourceName(this ObjectTypePair source, out string? value)
    {
        return source.GetPropertyValue("ServerResourceName").TryCast(out value);
    }

    #endregion

    #region Other

    private static ObjectTypePair WithType(this object obj)
    {
        return new(obj, obj.GetType());
    }

    private static object? GetPropertyValue(this ObjectTypePair source, string name)
    {
        return source.Type.GetProperty(name) is { CanRead: true } property
            ? property.GetValue(source.Object)
            : null;
    }

    private static bool TryCast<T>(this object? obj, out T? value)
        where T : class
    {
        switch (obj)
        {
            case null: value = null; return true;
            case T v:  value = v;    return true;
            default:   value = null; return false;
        }
    }

    private static ArgumentException OnFailure(object? obj, string objective)
    {
        return new ArgumentException(string.Format(
            "Cannot interpret {0} value as {1}.",
            obj is null ? "null" : obj.GetType().FullName,
            objective
        ));
    }

    // FUTURE: In PS 7.4 / .NET 8, this can be replaced with a tuple alias.
    //         In PS 7.2 / .NET 6, the tuple alias crahes a source generator.
    //
    // Example:
    // using ObjectTypePair = (object Object, Type Type);
    //
    private readonly record struct ObjectTypePair(object Object, Type Type);

    #endregion
}
