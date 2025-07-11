// Copyright Subatomix Research Inc.
// SPDX-License-Identifier: MIT

namespace PSql.Deploy.Seeds;

/// <summary>
///   Options for <see cref="SeedSession"/>.
/// </summary>
public class SeedSessionOptions
{
    /// <summary>
    ///   Gets or sets preprocessor variable definitions for the session.  Each
    ///   definition is a tuple of the variable name and its value.  The
    ///   default value is <see langword="null"/>.
    /// </summary>
    public IEnumerable<(string Name, string Value)>? Defines { get; set; }

    /// <summary>
    ///   Gets or sets whether the seeding session operates in what-if mode.
    ///   In this mode, a seeding session reports what actions it would perform
    ///   against a target database but does not perform the actions.  The
    ///   default value is <see langword="false"/>.
    /// </summary>
    public bool IsWhatIfMode { get; set; }

    /// <summary>
    ///   Gets or sets the maximum count of exceptions that the seeding session
    ///   should tolerate before cancelling ongoing operations.  Must be zero
    ///   or a positive number.  The default value is zero.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">
    ///   <paramref name="value"/> is negative.
    /// </exception>
    public int MaxErrorCount
    {
        get => _maxErrorCount;
        set
        {
            if (value < 0)
                throw new ArgumentOutOfRangeException(nameof(value));

            _maxErrorCount = value;
        }
    }
    private int _maxErrorCount;
}
