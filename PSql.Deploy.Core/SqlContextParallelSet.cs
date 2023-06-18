// Copyright 2023 Subatomix Research Inc.
// SPDX-License-Identifier: ISC

namespace PSql;

/// <summary>
///   A set of <see cref="SqlContext"/> objects to be processed in parallel.
/// </summary>
public class SqlContextParallelSet
{
    private IList<SqlContext>? _contexts;
    private int                _parallelism;

    /// <summary>
    ///   Initializes a new <see cref="SqlContextParallelSet"/> instance.
    /// </summary>
    public SqlContextParallelSet()
    {
        _parallelism = Environment.ProcessorCount;
    }

    /// <summary>
    ///   Initializes a new <see cref="SqlContextParallelSet"/> instance with
    ///   the specified contexts and maximum degree of parallelism.
    /// </summary>
    /// <param name="contexts">
    ///   The contexts to be in the set.
    /// </param>
    /// <param name="parallelism">
    ///   The maximum degree of parallelism with which to process the set.
    ///   Must be a positive integer.
    /// </param>
    /// <exception cref="ArgumentNullException">
    ///   <paramref name="contexts"/> is <see langword="null"/>.
    /// </exception>"
    /// <exception cref="ArgumentOutOfRangeException">
    ///   <paramref name="parallelism"/> is zero or negative.
    /// </exception>"
    public SqlContextParallelSet(IList<SqlContext> contexts, int parallelism)
    {
        if (contexts is null)
            throw new ArgumentNullException(nameof(contexts));
        if (parallelism < 1)
            throw new ArgumentOutOfRangeException(nameof(parallelism));

        _contexts    = contexts;
        _parallelism = parallelism;
    }

    /// <summary>
    ///   Gets or sets a descriptive name for the set.  The default value is
    ///   <see langword="null"/>.
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    ///   Gets or sets the list of contexts in the set.  The default value is
    ///   an empty list.
    /// </summary>
    /// <exception cref="ArgumentNullException">
    ///   Attempted to set the property to <see langword="null"/>.
    /// </exception>
    public IList<SqlContext> Contexts
    {
        get => _contexts ??= new List<SqlContext>();
        set
        {
            if (value is null)
                throw new ArgumentNullException(nameof(value));

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
        get => _parallelism;
        set
        {
            if (value < 1)
                throw new ArgumentOutOfRangeException(nameof(value));

            _parallelism = value;
        }
    }
}
