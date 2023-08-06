// Copyright 2023 Subatomix Research Inc.
// SPDX-License-Identifier: ISC

namespace PSql.Deploy;

/// <summary>
///   Represents a <see cref="SqlContext"/> along with computed display names
///   for the server and database it represents.
/// </summary>
public class SqlContextWork
{
    /// <summary>
    ///   Initializes a new <see cref="SqlContextWork"/> instance for the
    ///   specified context.
    /// </summary>
    /// <param name="context">
    ///   The context to represent.
    /// </param>
    /// <exception cref="ArgumentNullException">
    ///   <paramref name="context"/> is <see langword="null"/>.
    /// </exception>
    public SqlContextWork(SqlContext context)
    {
        if (context is null)
            throw new ArgumentNullException(nameof(context));

        Context = context;

        ServerDisplayName
            =  context.AsAzure?.ServerResourceName
            ?? context.ServerName
            ?? "local";

        DatabaseDisplayName
            =  context.DatabaseName
            ?? "default";

        FullDisplayName = string.Concat(
            ServerDisplayName, ".",
            DatabaseDisplayName
        );
    }

    /// <summary>
    ///   Gets an object specifying how to connect to the database.
    /// </summary>
    public SqlContext Context { get; }

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
