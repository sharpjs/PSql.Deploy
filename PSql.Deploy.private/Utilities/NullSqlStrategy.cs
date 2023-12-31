// Copyright 2023 Subatomix Research Inc.
// SPDX-License-Identifier: ISC

using PSql.Internal;

namespace PSql.Deploy;

/// <remarks>
///   This implementation does nothing and is intended to support the
///   <c>-WhatIf</c> switch.
/// </remarks>
/// <inheritdoc cref="ISqlStrategy"/>
internal class NullSqlStrategy : ISqlStrategy
{
    private NullSqlStrategy() { }

    /// <summary>
    ///   Gets the singleton <see cref="NullSqlStrategy"/> instance.
    /// </summary>
    public static NullSqlStrategy Instance { get; } = new();

    /// <inheritdoc/>
    public Task<ISqlConnection> ConnectAsync(
        SqlContext        context,
        ISqlMessageLogger logger,
        CancellationToken cancellation)
    {
        return Task.FromResult<ISqlConnection>(new NullSqlConnection());
    }

    /// <inheritdoc/>
    public Task ExecuteNonQueryAsync(
        ISqlCommand       command,
        CancellationToken cancellation)
    {
        return Task.CompletedTask;
    }
}
