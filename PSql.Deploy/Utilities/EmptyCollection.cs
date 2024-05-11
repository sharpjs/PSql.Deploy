// Copyright 2024 Subatomix Research Inc.
// SPDX-License-Identifier: ISC

using System.Collections;

namespace PSql.Deploy.Utilities;

/// <summary>
///   A collection that is always empty.
/// </summary>
/// <remarks>
///   The mutation methods do not throw but instead discard their inputs.
/// </remarks>
/// <typeparam name="T">
///   The type of item (not) in the collection.
/// </typeparam>
internal class EmptyCollection<T> : ICollection<T>, IReadOnlyCollection<T>
{
    /// <summary>
    ///   Gets the singleton instance of <see cref="EmptyCollection{T}"/>.
    /// </summary>
    public static EmptyCollection<T> Instance { get; } = new();

    /// <summary>
    ///   Initializes a new <see cref="EmptyCollection{T}"/> instance.
    /// </summary>
    protected EmptyCollection() { }

    /// <inheritdoc/>
    public int Count => 0;

    /// <inheritdoc/>
    public bool IsReadOnly => true;

    /// <inheritdoc/>
    public void Add(T item) { }

    /// <inheritdoc/>
    public void Clear() { }

    /// <inheritdoc/>
    public bool Contains(T item) => false;

    /// <inheritdoc/>
    public void CopyTo(T[] array, int arrayIndex) { }

    /// <inheritdoc/>
    public bool Remove(T item) => false;

    /// <inheritdoc cref="IEnumerable{T}.GetEnumerator"/>
    public EmptyEnumerator<T> GetEnumerator() => EmptyEnumerator<T>.Instance;

    /// <inheritdoc/>
    IEnumerator<T> IEnumerable<T>.GetEnumerator() => GetEnumerator();

    /// <inheritdoc/>
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
