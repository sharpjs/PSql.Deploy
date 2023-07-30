// Copyright 2023 Subatomix Research Inc.
// SPDX-License-Identifier: ISC

namespace PSql.Deploy.Migrations;

/// <summary>
///   A session in which schema migrations are applied to a set of target
///   databases.
/// </summary>
internal interface IMigrationSession
{
    /// <summary>
    ///   Gets the defined migrations.
    /// </summary>
    ImmutableArray<Migration> Migrations { get; }

    /// <summary>
    ///   Gets the minimum (earliest) defined migration name, excluding the
    ///   <c>_Begin</c> and <c>_End</c> pseudo-migrations.  Returns an empty
    ///   empty string if if there are no defined migrations or if all of them
    ///   are pseudo-migrations.
    /// </summary>
    string MinimumMigrationName { get; }

    /// <summary>
    ///   Gets the current deployment phase.
    /// </summary>
    MigrationPhase Phase { get; }

    /// <summary>
    ///   Gets whether migration application to one or more target databases
    ///   failed with an error.
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
    ///   Reports the start of migration application to the specified target
    ///   database.
    /// </summary>
    /// <param name="databaseName">
    ///   The name of the target database.
    /// </param>
    void ReportStarting(string databaseName);

    /// <summary>
    ///   Reports the application of specified migration content to the
    ///   specified target database.
    /// </summary>
    /// <param name="databaseName">
    ///   The name of the target database.
    /// </param>
    /// <param name="migrationName">
    ///   The name of the migration.
    /// </param>
    /// <param name="phase">
    ///   The deployment phase that identifies the content of the migration.
    ///   This value usually equals <see cref="Phase"/> but can differ if
    ///   content is moved between phases to satisfy dependencies.
    /// </param>
    void ReportApplying(
        string         databaseName,
        string         migrationName,
        MigrationPhase phase);

    /// <summary>
    ///   Reports the end of migration application to the specified target
    ///   database.
    /// </summary>
    /// <param name="databaseName">
    ///   The name of the target database.
    /// </param>
    /// <param name="count">
    ///   The count of migrations that were applied.
    /// </param>
    /// <param name="duration">
    ///   The duration of migration application to the target database.
    /// </param>
    /// <param name="disposition">
    ///   The outcome of migration application to the target database.
    /// </param>
    void ReportApplied(
        string                     databaseName,
        int                        count,
        TimeSpan                   duration,
        MigrationTargetDisposition disposition);

    /// <summary>
    ///   Reports a problem.
    /// </summary>
    /// <param name="message">
    ///   A message that describes the problem.
    /// </param>
    void ReportProblem(string message);
}
