// Copyright 2023 Subatomix Research Inc.
// SPDX-License-Identifier: ISC

namespace PSql.Deploy.Migrations;

/// <summary>
///   A console to report the progress of schema migration application to a
///   particular target database.
/// </summary>
public interface IMigrationConsole
{
    /// <summary>
    ///   Reports the start of migration application to the target database.
    /// </summary>
    void ReportStarting();

    /// <summary>
    ///   Reports the application of the specified migration content to the
    ///   target database.
    /// </summary>
    /// <param name="migrationName">
    ///   The name of the migration.
    /// </param>
    /// <param name="phase">
    ///   The deployment phase that identifies the content of the migration.
    /// </param>
    void ReportApplying(string migrationName, MigrationPhase phase);

    /// <summary>
    ///   Reports the end of migration application to the target database.
    /// </summary>
    /// <param name="count">
    ///   The count of migrations that were applied.
    /// </param>
    /// <param name="duration">
    ///   The duration of migration application to the target database.
    /// </param>
    /// <param name="disposition">
    ///   The outcome of migration application to the target database.
    /// </param>
    void ReportApplied(int count, TimeSpan duration, MigrationTargetDisposition disposition);

    /// <summary>
    ///   Reports a problem.
    /// </summary>
    /// <param name="message">
    ///   A message that describes the problem.
    /// </param>
    void ReportProblem(string message);
}
