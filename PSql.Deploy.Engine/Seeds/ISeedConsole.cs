// Copyright Subatomix Research Inc.
// SPDX-License-Identifier: MIT

namespace PSql.Deploy.Seeds;

/// <summary>
///   A user interface to report the progress of content seed application.
/// </summary>
public interface ISeedConsole
{
    /// <summary>
    ///   Reports the start of seed application to a target database.
    /// </summary>
    /// <param name="info">
    ///   Information about the content seed application.
    /// </param>
    void ReportStarting(ISeedApplication info);

    /// <summary>
    ///   Reports the application of the specified seed module to a target
    ///   database.
    /// </summary>
    /// <param name="info">
    ///   Information about the content seed application.
    /// </param>
    /// <param name="moduleName">
    ///   The name of the seed module.
    /// </param>
    void ReportApplying(ISeedApplication info, string moduleName);

    /// <summary>
    ///   Reports the end of seed application to a target database.
    /// </summary>
    /// <param name="info">
    ///   Information about the content seed application.
    /// </param>
    /// <param name="count">
    ///   The count of seed modules that were applied.
    /// </param>
    /// <param name="duration">
    ///   The duration of the seed application.
    /// </param>
    /// <param name="disposition">
    ///   The outcome of the seed application.
    /// </param>
    void ReportApplied(ISeedApplication info,
        int count, TimeSpan duration, TargetDisposition disposition);

    /// <summary>
    ///   Reports a problem.
    /// </summary>
    /// <param name="info">
    ///   Information about the content seed application.
    /// </param>
    /// <param name="message">
    ///   A message that describes the problem.
    /// </param>
    void ReportProblem(ISeedApplication info, string message);

    /// <summary>
    ///   Creates a log file for application of the specified seed to the
    ///   specified target database.
    /// </summary>
    /// <param name="info">
    ///   Information about the content seed application.
    /// </param>
    /// <returns>
    ///   A writer that writes to the log file.
    /// </returns>
    /// <remarks>
    ///   Implementations must ensure that the returned writer is thread-safe.
    /// </remarks>
    /// <exception cref="ArgumentNullException">
    ///   <paramref name="info"/> is <see langword="null"/>.
    /// </exception>
    TextWriter CreateLog(ISeedApplication info);
}
