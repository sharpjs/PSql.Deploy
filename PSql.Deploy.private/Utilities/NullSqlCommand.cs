// Copyright 2023 Subatomix Research Inc.
// SPDX-License-Identifier: ISC

namespace PSql.Internal;

/// <summary>
///   An <see cref="ISqlCommand"/> implementation that does nothing.
/// </summary>
internal class NullSqlCommand : ISqlCommand
{
    /// <remarks>
    ///   This implementation always throws <see cref="NotSupportedException"/>.
    /// </remarks>
    /// <exception cref="NotSupportedException">
    ///   Always thrown.
    /// </exception>
    /// <inheritdoc/>
    DbCommand ISqlCommand.UnderlyingCommand
        => throw new NotSupportedException();

    /// <inheritdoc/>
    public string CommandText { get; set; } = "";

    /// <inheritdoc/>
    public int CommandTimeout { get; set; } = 30;

    /// <remarks>
    ///   This implementation does nothing and returns an empty enumerator.
    /// </remarks>
    /// <inheritdoc/>
    public IEnumerator<PSObject> ExecuteAndProjectToPSObjects(bool useSqlTypes = false)
        => Enumerable.Empty<PSObject>().GetEnumerator();

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
