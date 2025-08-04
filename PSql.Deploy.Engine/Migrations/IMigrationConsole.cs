// Copyright Subatomix Research Inc.
// SPDX-License-Identifier: MIT

namespace PSql.Deploy.Migrations;

/// <summary>
///   A user interface to report the progress of schema migration application.
/// </summary>
public interface IMigrationConsole
{
    /// <summary>
    ///   Reports the start of migration application to a target database.
    /// </summary>
    /// <param name="info">
    ///   Information about the migration application.
    /// </param>
    void ReportStarting(IMigrationApplication info);

    /// <summary>
    ///   Reports the application of the specified migration content to a
    ///   target database.
    /// </summary>
    /// <param name="info">
    ///   Information about the schema migration application.
    /// </param>
    /// <param name="migrationName">
    ///   The name of the migration being applied.
    /// </param>
    /// <param name="phase">
    ///   The phase that identifies the content of the migration being applied.
    /// </param>
    void ReportApplying(IMigrationApplication info, string migrationName, MigrationPhase phase);

    /// <summary>
    ///   Reports the end of migration application to a target database.
    /// </summary>
    /// <param name="info">
    ///   Information about the schema migration application.
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
    void ReportApplied(IMigrationApplication info,
        int count, TimeSpan duration, TargetDisposition disposition);

    /// <summary>
    ///   Reports a problem.
    /// </summary>
    /// <param name="info">
    ///   Information about the schema migration application.
    /// </param>
    /// <param name="message">
    ///   A message that describes the problem.
    /// </param>
    void ReportProblem(IMigrationApplication info, string message);

    /// <summary>
    ///   Creates a log for migration application to a target database.
    /// </summary>
    /// <param name="info">
    ///   Information about the schema migration application.
    /// </param>
    /// <returns>
    ///   A writer that writes to the log.
    /// </returns>
    TextWriter CreateLog(IMigrationApplication info);
}
