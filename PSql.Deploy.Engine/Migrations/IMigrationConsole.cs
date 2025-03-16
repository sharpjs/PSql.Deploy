// Copyright Subatomix Research Inc.
// SPDX-License-Identifier: MIT

namespace PSql.Deploy.Migrations;

/// <summary>
///   A user interface to report the progress of schema migration application.
/// </summary>
public interface IMigrationConsole
{
    /// <summary>
    ///   Reports the start of migration application to the specified target
    ///   database.
    /// </summary>
    /// <param name="target">
    ///   An object representing the target database.
    /// </param>
    void ReportStarting(Target target);

    /// <summary>
    ///   Reports the application of the specified migration content to the
    ///   specified target database.
    /// </summary>
    /// <param name="target">
    ///   An object representing the target database.
    /// </param>
    /// <param name="migrationName">
    ///   The name of the migration.
    /// </param>
    /// <param name="phase">
    ///   The phase that identifies the content of the migration.
    /// </param>
    void ReportApplying(Target target, string migrationName, MigrationPhase phase);

    /// <summary>
    ///   Reports the end of migration application to the specified target
    ///   database.
    /// </summary>
    /// <param name="target">
    ///   An object representing the target database.
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
    void ReportApplied(Target target, int count, TimeSpan duration, TargetDisposition disposition);

    /// <summary>
    ///   Reports a problem.
    /// </summary>
    /// <param name="target">
    ///   An object representing the target database to which the problem
    ///   applies, or <see langword="null"/> for a general problem.
    /// </param>
    /// <param name="message">
    ///   A message that describes the problem.
    /// </param>
    void ReportProblem(Target? target, string message);

    /// <summary>
    ///   Creates a log file for migration application to the specified target
    ///   database.
    /// </summary>
    /// <param name="target">
    ///   An object representing the target database.
    /// </param>
    /// <returns>
    ///   A writer that writes to the log file.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    ///   <paramref name="target"/> is <see langword="null"/>.
    /// </exception>
    TextWriter CreateLog(Target target);
}
