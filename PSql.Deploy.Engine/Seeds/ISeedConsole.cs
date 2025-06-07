// Copyright Subatomix Research Inc.
// SPDX-License-Identifier: MIT

namespace PSql.Deploy.Seeds;

/// <summary>
///   A user interface to report the progress of content seed application.
/// </summary>
public interface ISeedConsole
{
    /// <summary>
    ///   Reports the start of seed application to the specified target
    ///   database.
    /// </summary>
    /// <param name="target">
    ///   An object representing the target database.
    /// </param>
    void ReportStarting(Target target);

    /// <summary>
    ///   Reports the application of the specified seed module to the specified
    ///   target database.
    /// </summary>
    /// <param name="target">
    ///   An object representing the target database.
    /// </param>
    /// <param name="moduleName">
    ///   The name of the seed module.
    /// </param>
    void ReportApplying(Target target, string moduleName);

    /// <summary>
    ///   Reports the end of seed application to the specified target database.
    /// </summary>
    /// <param name="target">
    ///   An object representing the target database.
    /// </param>
    /// <param name="count">
    ///   The count of seeds that were applied.
    /// </param>
    /// <param name="duration">
    ///   The duration of seed application to the target database.
    /// </param>
    /// <param name="disposition">
    ///   The outcome of seed application to the target database.
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
    ///   Creates a log file for application of the specified seed to the
    ///   specified target database.
    /// </summary>
    /// <param name="target">
    ///   An object representing the target database.
    /// </param>
    /// <param name="seed">
    ///   The seed being applied to the target database.
    /// </param>
    /// <returns>
    ///   A writer that writes to the log file.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    ///   <paramref name="seed"/> and/or
    ///   <paramref name="target"/> is <see langword="null"/>.
    /// </exception>
    TextWriter CreateLog(Target target, Seed seed);
}
