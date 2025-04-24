// Copyright Subatomix Research Inc.
// SPDX-License-Identifier: MIT

using System.Collections;
using System.Net;
using System.Reflection;
using System.Security;

namespace PSql.Deploy;

using static BindingFlags;

internal static class TargetFactory
{
    internal static Target CreateFrom(object obj)
    {
        if (obj is null)
            throw new ArgumentNullException(nameof(obj));

        return ToTarget(obj)
            ?? throw new ArgumentException(
                $"Cannot interpret the '{obj.GetType().FullName}' object as a PSql.Deploy target."
            );
    }

    private static Target? ToTarget(object obj)
    {
        return obj is PSObject psObject
            ? ToTargetSpecialCase(psObject.BaseObject) ?? ToTargetFromPSObject(psObject)
            : ToTargetSpecialCase(obj)                 ?? ToTargetFromOther(obj);
    }

    private static Target? ToTargetSpecialCase(object obj)
    {
        if (obj is Target target)
            return target;

        if (obj is string text)
            return ToTargetFromString(text);

        if (obj is IDictionary dictionary)
            return ToTargetFromDictionary(dictionary);

        var type = obj.GetType();
        if (type.IsSqlContextType())
            return ToTargetFromSqlContext(obj, type);

        return null;
    }

    private static Target? ToTargetFromString(string text)
    {
        return text.Length > 0 ? new(connectionString: text) : null;
    }

    private static Target? ToTargetFromDictionary(IDictionary dictionary)
    {
        return ToTargetFromStructure(dictionary, GetDictionaryItem);
    }

    private static Target? ToTargetFromPSObject(PSObject psObject)
    {
        return ToTargetFromStructure(psObject, GetPSPropertyValue);
    }

    private static Target? ToTargetFromOther(object obj)
    {
        return ToTargetFromStructure((obj, obj.GetType()), GetPropertyValue);
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
        return type.FullName is { } n
            && n.StartsWith("PSql.",      StringComparison.Ordinal)
            && n.EndsWith  ("SqlContext", StringComparison.Ordinal);
    }

    private static Target? ToTargetFromSqlContext(object obj, Type type)
    {
        var version = SelectSqlClientVersion(type.Assembly);
        if (version is null)
            return null;

        var connectionString = obj
            .InvokeGetConnectionString(type, version)
            .ToNonEmptyString();
        if (connectionString is null)
            return null;

        var credential = obj
            .GetPropertyValue(type, "Credential")
            .ToCredential();

        var serverDisplayName = obj
            .GetPropertyValue(type, "ServerResourceName")
            .ToNonEmptyString();

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

    private static object? InvokeGetConnectionString(this object obj, Type type, object version)
    {
        var method = type.GetMethod(
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

        return method.Invoke(obj, [null, version, true]);
    }

    private static string? ToNonEmptyString(this object? obj)
    {
        return obj?.ToString().NullIfEmpty();
    }

    private static NetworkCredential? ToCredential(this object? obj)
    {
        if (obj is null)
            return null;

        return obj is PSObject pSObject
            ? ToCredentialSpecialCase(pSObject.BaseObject) ?? ToCredentialFromPSObject(pSObject)
            : ToCredentialSpecialCase(obj)                 ?? ToCredentialFromOther(obj);
    }

    private static NetworkCredential? ToCredentialSpecialCase(object obj)
    {
        if (obj is NetworkCredential credential)
            return credential;

        if (obj is PSCredential psCredential && psCredential != PSCredential.Empty)
            return psCredential.GetNetworkCredential();

        if (obj is IDictionary dictionary)
            return ToCredentialFromDictionary(dictionary);

        return null;
    }

    private static NetworkCredential? ToCredentialFromDictionary(IDictionary dictionary)
    {
        return ToCredentialFromStructure(dictionary, GetDictionaryItem);
    }

    private static NetworkCredential? ToCredentialFromPSObject(PSObject pSObject)
    {
        return ToCredentialFromStructure(pSObject, GetPSPropertyValue);
    }

    private static NetworkCredential? ToCredentialFromOther(object obj)
    {
        return ToCredentialFromStructure((obj, obj.GetType()), GetPropertyValue);
    }

    private static NetworkCredential? ToCredentialFromStructure<T>(T source, Func<T, string, object?> accessor)
    {
        var username
            =  accessor(source, "UserId")  .ToNonEmptyString()
            ?? accessor(source, "Username").ToNonEmptyString()
            ?? accessor(source, "UserName").ToNonEmptyString();
        if (username is null)
            return null;

        var password = accessor(source, "Password");

        if (password is SecureString securePassword)
            return new(username, securePassword);

        if (password.ToNonEmptyString() is { } textPassword)
            return new(username, textPassword);

        return null;
    }

    private static object? GetDictionaryItem(IDictionary dictionary, string key)
    {
        return dictionary[key];
    }

    private static object? GetPSPropertyValue(PSObject obj, string name)
    {
        return obj.Properties[name] is { IsGettable: true } property
            ? property.Value
            : null;
    }

    private static object? GetPropertyValue((object obj, Type type) source, string name)
    {
        return source.obj.GetPropertyValue(source.type, name);
    }

    private static object? GetPropertyValue(this object obj, Type type, string name)
    {
        return type.GetProperty(name) is { CanRead: true } property
            ? property.GetValue(obj)
            : null;
    }
}
