// Copyright 2023 Subatomix Research Inc.
// SPDX-License-Identifier: ISC

namespace PSql.Deploy.Migrations;

/// <summary>
///   Information about a session in which schema migrations are applied to a
///   set of target databases.
/// </summary>
/// <remarks>
///   This interface is used by lower-level code that consumes the session
///   information.  Higher-level code that controls the operation of the
///   session uses <see cref="IMigrationSessionControl"/> instead.
/// </remarks>
public interface IMigrationSession
{
    /// <summary>
    ///   Gets the defined migrations.
    /// </summary>
    ImmutableArray<Migration> Migrations { get; }

    /// <summary>
    ///   Gets the earliest (minimum) name of the migrations in
    ///   <see cref="Migrations"/>, excluding the <c>_Begin</c> and <c>_End</c>
    ///   pseudo-migrations if present.  Returns an empty empty string if
    ///   <see cref="Migrations"/> is empty or contains only pseudo-migrations.
    /// </summary>
    string EarliestDefinedMigrationName { get; }

    /// <summary>
    ///   Gets the current deployment phase.
    /// </summary>
    MigrationPhase Phase { get; }

    /// <summary>
    ///   Gets whether to allow a non-skippable <c>Core</c> phase.
    /// </summary>
    bool AllowCorePhase { get; }

    /// <summary>
    ///   Gets whether to operate in what-if mode.  In this mode, code should
    ///   report what actions it would perform against a target database but
    ///   should not perform the actions.
    /// </summary>
    bool IsWhatIfMode { get; }

    /// <summary>
    ///   Gets whether migration application to one or more target databases
    ///   encountered an error.
    /// </summary>
    bool HasErrors { get; }

    /// <summary>
    ///   Gets the token to monitor for cancellation requests.
    /// </summary>
    CancellationToken CancellationToken { get; }

    /// <summary>
    ///   Creates a log file.
    /// </summary>
    /// <param name="fileName">
    ///   The name of the log file.
    /// </param>
    /// <returns>
    ///   A writer that writes to the log file.
    /// </returns>
    TextWriter CreateLog(string fileName);

    /// <summary>
    ///   Gets migrations applied to the specified target database
    ///   asynchronously.
    /// </summary>
    /// <param name="context">
    ///   An object specifying how to connect to the target database.
    /// </param>
    /// <param name="logger">
    ///   The object to use to log messages received from the target database
    ///   server.
    /// </param>
    /// <returns>
    ///   A <see cref="Task"/> representing the asynchronous operation.  When
    ///   the task completes, its <see cref="Task{TResult}.Result"/> property
    ///   contains the migrations registered in the database specified by
    ///   <paramref name="context"/>.
    /// </returns>
    Task<IReadOnlyList<Migration>> GetAppliedMigrationsAsync(
        SqlContext context, ISqlMessageLogger logger);
}
