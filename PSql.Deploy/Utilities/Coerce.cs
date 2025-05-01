// Copyright Subatomix Research Inc.
// SPDX-License-Identifier: MIT

using System.Collections;

namespace PSql.Deploy;

using ObjectTypePair = (object Object, Type Type); // FUTURE: make global in PS 7.4 / .NET 8

/// <summary>
///   Methods for cmdlet argument coercion.
/// </summary>
internal static partial class Coerce
{
    private static readonly Func<IDictionary, string, object?>
        DictionaryAccessor = GetDictionaryValue;

    private static readonly Func<PSObject, string, object?>
        PSObjectAccessor = GetPSPropertyValue;

    private static readonly Func<ObjectTypePair, string, object?>
        ObjectAccessor = GetPropertyValue;

    private static string? ToNonEmptyString(this object? obj)
    {
        return obj?.ToString().NullIfEmpty();
    }

    private static ObjectTypePair WithType(this object obj)
    {
        return (obj, obj.GetType());
    }

    private static object? GetDictionaryValue(this IDictionary dictionary, string key)
    {
        return dictionary[key];
    }

    private static object? GetPSPropertyValue(this PSObject obj, string name)
    {
        return obj.Properties[name] is { IsGettable: true } property
            ? property.Value
            : null;
    }

    private static object? GetPropertyValue(this ObjectTypePair source, string name)
    {
        return source.Type.GetProperty(name) is { CanRead: true } property
            ? property.GetValue(source.Object)
            : null;
    }

    private static InvalidCastException OnFailure(object? obj, string what)
    {
        return new InvalidCastException(string.Format(
            "Cannot interpret {0} value as {1}.",
            obj?.GetType().FullName ?? "null",
            what
        ));
    }
}
