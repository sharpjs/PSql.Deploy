// Copyright Subatomix Research Inc.
// SPDX-License-Identifier: MIT

using System.Net;

namespace PSql.Deploy;

/// <summary>
///   Represents a target database.
/// </summary>
public class Target
{
    /// <summary>
    ///   Initializes a new <see cref="Target"/> instance.
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
    /// <exception cref="ArgumentException">
    ///   <paramref name="connectionString"/> is not a valid connection string.
    /// </exception>
    public Target(
        string             connectionString,
        NetworkCredential? credential          = null,
        string?            serverDisplayName   = null,
        string?            databaseDisplayName = null)
    {
        if (connectionString is null)
            throw new ArgumentNullException(nameof(connectionString));

        ConnectionString = connectionString;
        Credential       = credential;
        SqlCredential    = ConvertCredential(credential);

        var builder = new SqlConnectionStringBuilder(connectionString);

        ServerDisplayName
            =  serverDisplayName
            ?? builder.DataSource.NullIfEmpty().NullIf(".")
            ?? "local";

        DatabaseDisplayName
            =  databaseDisplayName
            ?? builder.InitialCatalog.NullIfEmpty()
            ?? "default";

        FullDisplayName = string.Concat(
            ServerDisplayName, ".",
            DatabaseDisplayName
        );
    }

    private static SqlCredential? ConvertCredential(NetworkCredential? credential)
    {
        if (credential is null)
            return null;

        var password = credential.SecurePassword;

        if (!password.IsReadOnly())
        {
            password = password.Copy();
            password.MakeReadOnly();
        }

        return new(credential.UserName, password);
    }

    /// <summary>
    ///   Gets the SqlClient connection string for the target database.
    /// </summary>
    public string ConnectionString { get; }

    /// <summary>
    ///   Gets the credential to use to authenticate with the target database,
    ///   if a credential is required and not present in the
    ///   <see cref="ConnectionString"/>.
    /// </summary>
    public NetworkCredential? Credential { get; }

    /// <inheritdoc cref="Credential"/>
    internal SqlCredential? SqlCredential { get; }

    /// <summary>
    ///   Gets a display name for the database server.  This name might be a
    ///   DNS name, an Azure resource name, or a placeholder indicating a local
    ///   SQL Server instance.
    /// </summary>
    public string ServerDisplayName { get; }

    /// <summary>
    ///   Gets a display name for the database.  This name might be a real
    ///   database name or a placeholder indicating the default database for
    ///   the connection.
    /// </summary>
    public string DatabaseDisplayName { get; }

    /// <summary>
    ///   Gets the full display name for server and database.  This value
    ///   contains both <see cref="ServerDisplayName"/> and
    ///   <see cref="DatabaseDisplayName"/>.
    /// </summary>
    public string FullDisplayName { get; }
}
