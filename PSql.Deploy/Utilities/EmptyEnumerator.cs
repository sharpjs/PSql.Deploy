// Copyright 2024 Subatomix Research Inc.
// SPDX-License-Identifier: ISC

using System.Collections;

namespace PSql.Deploy.Utilities;

/// <summary>
///   An enumerator that is always empty.
/// </summary>
/// <typeparam name="T">
///   The type of item (not) in the enumerator.
/// </typeparam>
internal class EmptyEnumerator<T> : IEnumerator<T>
{
    /// <summary>
    ///   Gets the singleton instance of <see cref="EmptyEnumerator{T}"/>.
    /// </summary>
    public static EmptyEnumerator<T> Instance { get; } = new();

    protected EmptyEnumerator() { }

    /// <inheritdoc/>
    public T Current => default!;

    /// <inheritdoc/>
    object IEnumerator.Current => default!;

    /// <inheritdoc/>
    public bool MoveNext() => false;

    /// <inheritdoc/>
    public void Reset() { }

    /// <inheritdoc/>
    public void Dispose() { }
}
