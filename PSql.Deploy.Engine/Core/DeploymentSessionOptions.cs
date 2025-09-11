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
    ///   Gets or sets the maximum number of actions (such as SQL batches) that
    ///   the session should execute in parallel across all target databases.
    ///   Must be a positive number.  The default value is
    ///   <see cref="int.MaxValue"/>.
    /// </summary>
    /// <remarks>
    ///   This limit applies in addition to any group-specific limits imposed
    ///   by <see cref="TargetGroup.MaxParallelism"/>.
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException">
    ///   <paramref name="value"/> is zero or negative.
    /// </exception>
    public int MaxParallelism
    {
        get => _maxParallelism;
        set
        {
            if (value <= 0)
                throw new ArgumentOutOfRangeException(nameof(value));

            _maxParallelism = value;
        }
    }
    private int _maxParallelism = int.MaxValue;

    /// <summary>
    ///   Gets or sets the maximum number of actions (such as SQL batches) that
    ///   the session should execute in parallel against any one target
    ///   database.  Must be a positive number.  The default value is
    ///   <see cref="int.MaxValue"/>.
    /// </summary>
    /// <remarks>
    ///   This limit applies in addition to any group-specific limits imposed
    ///   by <see cref="TargetGroup.MaxParallelismPerTarget"/>.
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException">
    ///   <paramref name="value"/> is zero or negative.
    /// </exception>
    public int MaxParallelismPerTarget
    {
        get => _maxParallelismPerTarget;
        set
        {
            if (value <= 0)
                throw new ArgumentOutOfRangeException(nameof(value));

            _maxParallelismPerTarget = value;
        }
    }
    private int _maxParallelismPerTarget = int.MaxValue;

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
