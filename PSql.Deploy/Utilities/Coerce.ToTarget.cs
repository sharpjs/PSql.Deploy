// Copyright Subatomix Research Inc.
// SPDX-License-Identifier: MIT

using System.Collections;
using System.Reflection;

namespace PSql.Deploy;

using static BindingFlags;

using ObjectTypePair = (object Object, Type Type); // FUTURE: make global in PS 7.4 / .NET 8

partial class Coerce // -> Target
{
    internal static Target ToTargetRequired(object? obj)
    {
        return ToTarget(obj) ?? throw OnFailure(obj, "a target database");
    }

    internal static Target? ToTarget(object? obj)
    {
        if (obj == null)
            return null;

        return obj is PSObject psObject
            ? ToTargetSpecialCase(psObject.BaseObject) ?? ToTargetFromPSObject(psObject)
            : ToTargetSpecialCase(obj)                 ?? ToTargetFromOther(obj);
    }

    private static Target? ToTargetSpecialCase(object obj)
    {
        if (obj is Target target)
            return target;

        if (obj is string text)
            return ToTargetFromConnectionString(text);

        if (obj is IDictionary dictionary)
            return ToTargetFromDictionary(dictionary);

        var type = obj.GetType();
        if (IsSqlContextType(type))
            return ToTargetFromSqlContext((obj, type));

        return null;
    }

    private static Target? ToTargetFromConnectionString(string connectionString)
    {
        return connectionString.Length > 0 ? new(connectionString) : null;
    }

    private static Target? ToTargetFromDictionary(IDictionary dictionary)
    {
        return ToTargetFromStructure(dictionary, DictionaryAccessor);
    }

    private static Target? ToTargetFromPSObject(PSObject psObject)
    {
        return ToTargetFromStructure(psObject, PSObjectAccessor);
    }

    private static Target? ToTargetFromOther(object obj)
    {
        return ToTargetFromStructure(obj.WithType(), ObjectAccessor);
    }

    private static Target? ToTargetFromStructure<T>(T source, Func<T, string, object?> accessor)
    {
        var connectionString = accessor(source, "ConnectionString").ToNonEmptyString();
        if (connectionString is null)
            return null;

        var credential = accessor(source, "Credential").ToCredential();

        var serverDisplayName
            =  accessor(source, "ServerDisplayName").ToNonEmptyString()
            ?? accessor(source, "ServerName")       .ToNonEmptyString()
            ?? accessor(source, "Server")           .ToNonEmptyString();

        var databaseDisplayName
            =  accessor(source, "DatabaseDisplayName").ToNonEmptyString()
            ?? accessor(source, "DatabaseName")       .ToNonEmptyString()
            ?? accessor(source, "Database")           .ToNonEmptyString();

        return new(connectionString, credential, serverDisplayName, databaseDisplayName);
    }

    private static bool IsSqlContextType(this Type type)
    {
        return type.FullName is { } name
            && name.StartsWith("PSql.",      StringComparison.Ordinal)
            && name.EndsWith  ("SqlContext", StringComparison.Ordinal);
    }

    private static Target? ToTargetFromSqlContext(ObjectTypePair source)
    {
        var version = SelectSqlClientVersion(source.Type.Assembly);
        if (version is null)
            return null;

        var connectionString = source.InvokeGetConnectionString(version).ToNonEmptyString();
        if (connectionString is null)
            return null;

        var credential        = source.GetPropertyValue("Credential")        .ToCredential();
        var serverDisplayName = source.GetPropertyValue("ServerResourceName").ToNonEmptyString();

        return new(connectionString, credential, serverDisplayName);
    }

    private static object? SelectSqlClientVersion(Assembly assembly)
    {
        const string SqlClientVersionName = "Mds5";

        var type = assembly.GetType("PSql.SqlClientVersion");

        if (type is not { IsEnum: true })
            return null;

        if (!Enum.TryParse(type, SqlClientVersionName, out var value))
            return null;

        return value;
    }

    private static object? InvokeGetConnectionString(this ObjectTypePair source, object version)
    {
        var method = source.Type.GetMethod(
            name:                  "GetConnectionString",
            genericParameterCount: 0,
            bindingAttr:           Public | Instance,
            binder:                null,
            types: [
                typeof(string),    // databaseName
                version.GetType(), // sqlClientVersion
                typeof(bool)       // omitCredential
            ],
            modifiers:             null
        );

        if (method?.ReturnType != typeof(string))
            return null;

        return method.Invoke(source.Object, [null, version, true]);
    }
}
