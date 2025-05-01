// Copyright Subatomix Research Inc.
// SPDX-License-Identifier: MIT

using System.Net;

namespace PSql.Deploy;

/// <summary>
///   Represents a target database.
/// </summary>
public class Target
{
    private readonly E.Target _target;

    internal Target(
        string             connectionString,
        NetworkCredential? credential          = null,
        string?            serverDisplayName   = null,
        string?            databaseDisplayName = null)
    {
        _target = new(connectionString, credential, serverDisplayName, databaseDisplayName);

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
    public NetworkCredential? Credential { get; }

    /// <summary>
    ///   Gets a display name for the database server.  This name might be a
    ///   DNS name, an Azure resource name, or a placeholder indicating a local
    ///   SQL Server instance.
    /// </summary>
    public string? ServerDisplayName => _target.ServerDisplayName;

    /// <summary>
    ///   Gets a display name for the database.  This name might be a real
    ///   database name or a placeholder indicating the default database for
    ///   the connection.
    /// </summary>
    public string? DatabaseDisplayName => _target.DatabaseDisplayName;

    /// <summary>
    ///   Gets the full display name for server and database.  This value
    ///   contains both <see cref="ServerDisplayName"/> and
    ///   <see cref="DatabaseDisplayName"/>.
    /// </summary>
    public string FullDisplayName => _target.FullDisplayName;
}
