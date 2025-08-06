// Copyright Subatomix Research Inc.
// SPDX-License-Identifier: MIT

using System.Diagnostics;
using System.Net;

using static System.Reflection.BindingFlags;

namespace PSql.Deploy;

/// <summary>
///   Represents a target database.
/// </summary>
[DebuggerDisplay(@"\{{FullDisplayName}\}")]
public class SqlTargetDatabase
{
    private readonly E.Target _target;

    /// <summary>
    ///   Initializes a new <see cref="SqlTargetDatabase"/> instance by converting from
    ///   the specified object.
    /// </summary>
    /// <param name="obj">
    ///   The object to convert into a <see cref="SqlTargetDatabase"/>.
    /// </param>
    /// <exception cref="ArgumentNullException">
    ///   <see langword="object"/> is <see langword="null"/>.
    /// </exception>
    /// <exception cref="ArgumentException">
    ///   <see langword="object"/> is not convertible to <see cref="SqlTargetDatabase"/>.
    /// </exception>
    public SqlTargetDatabase(object obj)
    {
        if (obj == null)
            throw new ArgumentNullException(nameof(obj));

        if (obj is PSObject pSObject)
            obj = pSObject.BaseObject;

        if (obj is SqlTargetDatabase target)
            (_target, Credential) = (target.InnerTarget, target.Credential);

        else if (obj is string connectionString)
            _target = new(connectionString);

        else if (IsSqlContext(obj, out var type))
            (_target, Credential) = InterpretSqlContext(new ObjectTypePair(obj, type));

        else
            throw new ArgumentException(
                "Unsupported conversion.  To convert to PSql.Deploy.Target, " +
                "supply either a connection string or a PSql.SqlContext object " +
                "created with the New-SqlContext cmdlet."
            );
    }

    /// <summary>
    ///   Initializes a new <see cref="SqlTargetDatabase"/> instance with the specified
    ///   values.
    /// </summary>
    /// <param name="connectionString">
    ///   The SqlClient connection string for the target database.
    /// </param>
    /// <param name="credential">
    ///   The credential to use to authenticate with the target database, or
    ///   <see langword="null"/> if a credential is not required or is present
    ///   present in <paramref name="connectionString"/>.
    /// </param>
    /// <param name="serverDisplayName">
    ///   A display name for the database server.  If this paraneter is
    ///   <see langword="null"/>, the display name is inferred from the
    ///   <paramref name="connectionString"/>.
    /// </param>
    /// <param name="databaseDisplayName">
    ///   A display name for the database.  If this paraneter is
    ///   <see langword="null"/>, the display name is inferred from the
    ///   <paramref name="connectionString"/>.
    /// </param>
    /// <exception cref="ArgumentNullException">
    ///   <paramref name="connectionString"/> is <see langword="null"/>.
    /// </exception>
    public SqlTargetDatabase(
        string        connectionString,
        PSCredential? credential          = null,
        string?       serverDisplayName   = null,
        string?       databaseDisplayName = null)
    {
        // Null checked by inner Target constructor

        _target = new(
            connectionString,
            credential?.GetNetworkCredential(),
            serverDisplayName,
            databaseDisplayName
        );

        Credential = credential;
    }

    /// <summary>
    ///   Gets the inner target wrapped by this object.
    /// </summary>
    internal E.Target InnerTarget => _target;

    /// <summary>
    ///   Gets the SqlClient connection string for the target database.
    /// </summary>
    public string ConnectionString => _target.ConnectionString;

    /// <summary>
    ///   Gets the credential to use to authenticate with the target database,
    ///   if a credential is required and not present in the
    ///   <see cref="ConnectionString"/>.
    /// </summary>
    public PSCredential? Credential { get; }

    /// <summary>
    ///   Gets a display name for the database server.  This name might be a
    ///   DNS name, an Azure resource name, or a placeholder indicating a local
    ///   SQL Server instance.
    /// </summary>
    public string ServerDisplayName => _target.ServerDisplayName;

    /// <summary>
    ///   Gets a display name for the database.  This name might be a real
    ///   database name or a placeholder indicating the default database for
    ///   the connection.
    /// </summary>
    public string DatabaseDisplayName => _target.DatabaseDisplayName;

    /// <summary>
    ///   Gets the full display name for server and database.  This value
    ///   contains both <see cref="ServerDisplayName"/> and
    ///   <see cref="DatabaseDisplayName"/>.
    /// </summary>
    public string FullDisplayName => _target.FullDisplayName;

    private static bool IsSqlContext(object obj, out Type type)
    {
        type = obj.GetType();

        return type.FullName is { } name
            && name.StartsWith("PSql.",      StringComparison.Ordinal)
            && name.EndsWith  ("SqlContext", StringComparison.Ordinal);
    }

    private static (E.Target, PSCredential?) InterpretSqlContext(ObjectTypePair source)
    {
        return TryGetConnectionString  (source, out var connectionString)  // string
            && TryGetCredential        (source, out var credential)        // NetworkCredential?
            && TryGetServerResourceName(source, out var serverDisplayName) // string?
            ? (
                new(connectionString, credential?.GetNetworkCredential(), serverDisplayName),
                credential
            )
            : throw new ArgumentException(
                "The object does not conform to the expected API surface of PSql.SqlContext."
            );
    }

    private static bool TryGetConnectionString(
        ObjectTypePair source, [MaybeNullWhen(false)] out string value)
    {
        const string
            VersionTypeName  = "PSql.SqlClientVersion",
            VersionValueName = "Mds5", // for Microsoft.Data.SqlClient v5
            MethodName       = "GetConnectionString";

        value = default;

        if (source.Type.Assembly.GetType(VersionTypeName) is not { IsEnum: true } versionType)
            return false; // version enum not found

        if (!Enum.TryParse(versionType, VersionValueName, out var version))
            return false; // version enum value not found

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
            return false; // method not found or returns wrong type

        if (method.Invoke(source.Object, [null, version, true]) is not string connectionString)
            return false; // method returned null

        value = connectionString;
        return true;
    }

    private static bool TryGetCredential(ObjectTypePair source, out PSCredential? value)
    {
        const string PropertyName = "Credential";

        value = default;

        if (source.Type.GetProperty(PropertyName) is not {} property)
            return false; // property does not exist

        if (!property.CanRead)
            return false; // property is not readable

        if (property.GetValue(source.Object) is not { } objectValue)
            return true; // property value is null; normal when credential is not required

        if (objectValue is not PSCredential typedValue)
            return false; // property value is of wrong type

        value = typedValue;
        return true;
    }

    private static bool TryGetServerResourceName(ObjectTypePair source, out string? value)
    {
        const string PropertyName = "ServerResourceName";

        value = default;

        if (source.Type.GetProperty(PropertyName) is not { } property)
            return true; // property does not exist; normal for non-Azure SqlClient

        if (!property.CanRead)
            return false; // property is not readable

        if (property.GetValue(source.Object) is not { } objectValue)
            return true; // property value is null; allowed by AzureSqlClient

        if (objectValue is not string typedValue)
            return false; // property value is of wrong type

        value = typedValue;
        return true;
    }
}
