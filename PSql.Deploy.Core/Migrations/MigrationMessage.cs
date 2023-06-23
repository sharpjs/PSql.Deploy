// Copyright 2023 Subatomix Research Inc.
// SPDX-License-Identifier: ISC

namespace PSql.Deploy.Migrations;

/// <summary>
///   A notification about a migration run.
/// </summary>
public abstract class MigrationMessage
{
    /// <summary>
    ///   Initializes a new <see cref="MigrationMessage"/> instance.
    /// </summary>
    protected MigrationMessage(TimeSpan totalElapsed)
    {
        TotalElapsed = totalElapsed;
    }

    /// <summary>
    ///   Gets or sets the total time elapsed in the migration run.
    /// </summary>
    public TimeSpan TotalElapsed { get; }

    /// <inheritdoc/>
    public override string ToString()
        //          ^^^^^^
        // This override changes the return type from string? to string
        => base.ToString()!;
}
