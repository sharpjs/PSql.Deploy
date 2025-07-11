// Copyright Subatomix Research Inc.
// SPDX-License-Identifier: MIT

namespace PSql.Deploy;

/// <summary>
///   Options for deployment sessions.
/// </summary>
/// <remarks>
///   This is the base class of
///   <see cref="Migrations.MigrationSessionOptions"/> and
///   <see cref="Seeds.SeedSessionOptions"/>.
/// </remarks>
public abstract class DeploymentSessionOptions
{
    /// <summary>
    ///   Gets or sets whether the session operates in what-if mode.  In this
    ///   mode, a session reports what actions it would perform against a
    ///   target database but does not perform the actions.  The default value
    ///   is <see langword="false"/>.
    /// </summary>
    public bool IsWhatIfMode { get; set; }

    /// <summary>
    ///   Gets or sets the maximum count of exceptions that the session should
    ///   tolerate before cancelling ongoing operations.  Must be zero or a
    ///   positive number.  The default value is zero.
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
