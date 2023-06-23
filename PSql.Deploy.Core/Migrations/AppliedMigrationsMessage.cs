// Copyright 2023 Subatomix Research Inc.
// SPDX-License-Identifier: ISC

namespace PSql.Deploy.Migrations;

/// <summary>
///   A notification that all applicable migrations have been applied to a
///   target database in a particular phase.
/// </summary>
public class AppliedMigrationsMessage : MigrationMessage
{
    /// <summary>
    ///   Initializes a new <see cref="AppliedMigrationsMessage"/> instance.
    /// </summary>
    public AppliedMigrationsMessage(
        int            count,
        SqlContext     target,
        MigrationPhase phase,
        TimeSpan       targetElapsed,
        TimeSpan       totalElapsed,
        Exception?     exception)
        : base(totalElapsed)
    {
        if (target is null)
            throw new ArgumentNullException(nameof(target));

        Count         = count;
        Target        = target;
        Phase         = phase;
        TargetElapsed = targetElapsed;
        Exception = exception;
    }

    /// <summary>
    ///   Gets the count of migrations that have been applied to the target
    ///   database in the current phase.
    /// </summary>
    public int Count { get; }

    /// <summary>
    ///   Gets an object describing the database to which migrations have been
    ///   applied.
    /// </summary>
    public SqlContext Target { get; }

    /// <summary>
    ///   Gets the current phase.
    /// </summary>
    public MigrationPhase Phase { get; }

    /// <summary>
    ///   Gets the duration that elapsed while applying migrations to the
    ///   target database.
    /// </summary>
    public TimeSpan TargetElapsed { get; }

    /// <summary>
    ///   Gets the exception, if any, that was thrown during the application of
    ///   migrations.
    /// </summary>
    public Exception? Exception { get; }

    /// <inheritdoc/>
    public override string ToString()
    {
        return string.Format(
            @"[+{0:hh\:mm\:ss}] {1}: Applied {2} {3} migration(s) in {4:N3} second(s){5}",
            TotalElapsed,
            Target.DatabaseName,
            Count,
            Phase,
            TargetElapsed,
            Exception is null ? null : " [EXCEPTION]"
        );
    }
}
