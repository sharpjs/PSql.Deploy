// Copyright 2023 Subatomix Research Inc.
// SPDX-License-Identifier: ISC

using System.Diagnostics.CodeAnalysis;

namespace PSql.Deploy.Migrations;

/// <summary>
///   A notification that a migration is being applied to a target database in
///   a particular phase.
/// </summary>
public class ApplyingMigrationMessage : MigrationMessage
{
    /// <summary>
    ///   Initializes a new <see cref="AppliedMigrationsMessage"/> instance.
    /// </summary>
    public ApplyingMigrationMessage(
        Migration      migration,
        SqlContext     target,
        MigrationPhase phase,
        TimeSpan       totalElapsed)
        : base(totalElapsed)
    {
        if (target is null)
            throw new ArgumentNullException(nameof(target));
        if (migration is null)
            throw new ArgumentNullException(nameof(migration));

        Target    = target;
        Migration = migration;
        Phase     = phase;
    }

    /// <summary>
    ///   Gets the migration that is being applied in the current phase.
    /// </summary>
    public Migration Migration { get; }

    /// <summary>
    ///   Gets an object describing the database to which the migration is
    ///   being applied.
    /// </summary>
    public SqlContext Target { get; }

    /// <summary>
    ///   Gets the current phase.
    /// </summary>
    public MigrationPhase Phase { get; }

    /// <inheritdoc/>
    public override string ToString()
    {
        return string.Format(
            @"[+{0:hh\:mm\:ss}] {1}: Applying {2} {3}",
            TotalElapsed,
            Target.DatabaseName,
            Migration.Name,
            Phase
        );
    }
}
