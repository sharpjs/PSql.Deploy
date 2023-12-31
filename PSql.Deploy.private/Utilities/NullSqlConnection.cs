// Copyright 2023 Subatomix Research Inc.
// SPDX-License-Identifier: ISC

namespace PSql.Internal;

/// <summary>
///   An <see cref="ISqlConnection"/> implementation that does nothing.
/// </summary>
public class NullSqlConnection : ISqlConnection
{
    /// <remarks>
    ///   This implementation always throws <see cref="NotSupportedException"/>.
    /// </remarks>
    /// <exception cref="NotSupportedException">
    ///   Always thrown.
    /// </exception>
    /// <inheritdoc/>
    DbConnection ISqlConnection.UnderlyingConnection
        => throw new NotSupportedException();

    /// <remarks>
    ///   This implementation always returns the empty string.
    /// </remarks>
    /// <inheritdoc/>
    public string ConnectionString => "";

    /// <remarks>
    ///   This implementation always returns <see langword="true"/>.
    /// </remarks>
    /// <inheritdoc/>
    public bool IsOpen => true;

    /// <remarks>
    ///   This implementation always returns <see langword="false"/>.
    /// </remarks>
    /// <inheritdoc/>
    public bool HasErrors => false;

    /// <remarks>
    ///   This implementation returns an <see cref="ISqlCommand"/>
    ///   implementation that does nothing.
    /// </remarks>
    /// <inheritdoc/>
    public ISqlCommand CreateCommand() => new NullSqlCommand();

    /// <remarks>
    ///   This implementation does nothing.
    /// </remarks>
    /// <inheritdoc/>
    public void ClearErrors() { }

    /// <remarks>
    ///   This implementation never throws.
    /// </remarks>
    /// <inheritdoc/>
    public void ThrowIfHasErrors() { }

    /// <remarks>
    ///   This implementation does nothing.
    /// </remarks>
    /// <inheritdoc/>
    public void Dispose() { }

    /// <remarks>
    ///   This implementation does nothing.
    /// </remarks>
    /// <inheritdoc/>
    public ValueTask DisposeAsync() => default;
}
