// Copyright 2023 Subatomix Research Inc.
// SPDX-License-Identifier: ISC

using System.Diagnostics.CodeAnalysis;

namespace PSql.Deploy;

/// <remarks>
///   This is the default implementation.
/// </remarks>
/// <inheritdoc cref="ISqlStrategy"/>
internal class DefaultSqlStrategy : ISqlStrategy
{
    private DefaultSqlStrategy() { }

    /// <summary>
    ///   Gets the singleton <see cref="DefaultSqlStrategy"/> instance.
    /// </summary>
    public static DefaultSqlStrategy Instance { get; } = new();

    /// <inheritdoc/>
    [ExcludeFromCodeCoverage] // Requires database
    public Task<ISqlConnection> ConnectAsync(
        SqlContext        context,
        ISqlMessageLogger logger,
        CancellationToken cancellation)
    {
        // TODO: Make async in PSql
        return Task.FromResult(context.Connect(databaseName: null, logger /*, cancellation*/));
    }

    /// <inheritdoc/>
    [ExcludeFromCodeCoverage] // Requires database
    public Task ExecuteNonQueryAsync(
        ISqlCommand       command,
        CancellationToken cancellation)
    {
        return command.UnderlyingCommand.ExecuteNonQueryAsync(cancellation);
    }
}
