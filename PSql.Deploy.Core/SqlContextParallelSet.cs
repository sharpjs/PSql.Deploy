// Copyright 2023 Subatomix Research Inc.
// SPDX-License-Identifier: ISC

namespace PSql;

/// <summary>
///   A set of <see cref="SqlContext"/> objects to be processed in parallel.
/// </summary>
public class SqlContextParallelSet
{
    private IReadOnlyList<SqlContext>? _contexts;
    private int                        _parallelism;

    /// <summary>
    ///   Gets or sets a descriptive name for the set.  The default value is
    ///   <see langword="null"/>.
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    ///   Gets or sets the contexts in the set.  The default value is an empty
    ///   collection.
    /// </summary>
    /// <exception cref="ArgumentNullException">
    ///   Attempted to set the property to <see langword="null"/>.
    /// </exception>
    /// <exception cref="ArgumentNullException">
    ///   Attempted to set the property to a collection with a
    ///   <see langword="null"/> element.
    /// </exception>
    public IReadOnlyList<SqlContext> Contexts
    {
        get => _contexts ??= Array.Empty<SqlContext>();
        set
        {
            if (value is null)
                throw new ArgumentNullException(nameof(value));
            if (value.Contains(null!))
                throw new ArgumentException("Cannot contain a null element.", nameof(value));

            _contexts = value;
        }
    }

    /// <summary>
    ///   Gets or sets the maximum degree of parallelism.  Must be a positive
    ///   integer.  The default value is the count of logical processors on the
    ///   current machine.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">
    ///   Attempted to set the property to zero or to a negative integer.
    /// </exception>
    public int Parallelism
    {
        get => _parallelism > 0 ? _parallelism : _parallelism = Environment.ProcessorCount;
        set
        {
            if (value < 1)
                throw new ArgumentOutOfRangeException(nameof(value));

            _parallelism = value;
        }
    }
}
