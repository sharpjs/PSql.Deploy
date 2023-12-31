// Copyright 2023 Subatomix Research Inc.
// SPDX-License-Identifier: ISC

namespace PSql.Deploy.Seeding;

/// <summary>
///   A console to report the progress of content seed application to a
///   particular target database.
/// </summary>
public interface ISeedConsole
{
    /// <summary>
    ///   Reports the start of seed application to the target database.
    /// </summary>
    void ReportStarting();

    /// <summary>
    ///   Reports the application of the specified seed module to the target
    ///   database.
    /// </summary>
    /// <param name="moduleName">
    ///   The name of the seed module.
    /// </param>
    void ReportApplying(string moduleName);

    /// <summary>
    ///   Reports the end of seed application to the target database.
    /// </summary>
    /// <param name="count">
    ///   The count of seeds that were applied.
    /// </param>
    /// <param name="duration">
    ///   The duration of seed application to the target database.
    /// </param>
    /// <param name="disposition">
    ///   The outcome of seed application to the target database.
    /// </param>
    void ReportApplied(int count, TimeSpan duration, TargetDisposition disposition);

    /// <summary>
    ///   Reports a problem.
    /// </summary>
    /// <param name="message">
    ///   A message that describes the problem.
    /// </param>
    void ReportProblem(string message);
}
