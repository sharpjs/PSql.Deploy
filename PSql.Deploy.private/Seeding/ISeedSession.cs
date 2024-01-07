// Copyright 2023 Subatomix Research Inc.
// SPDX-License-Identifier: ISC

namespace PSql.Deploy.Seeding;

/// <summary>
///   Information about a session in which content seeds are applied to a set
///   of target databases.
/// </summary>
/// <remarks>
///   This interface is used by lower-level code that consumes the session
///   information.  Higher-level code that controls the operation of the
///   session uses <see cref="ISeedSessionControl"/> instead.
/// </remarks>
public interface ISeedSession
{
    /// <summary>
    ///   Gets the seeds to be applied.
    /// </summary>
    ImmutableArray<Seed> Seeds { get; }

    /// <summary>
    ///   Gets whether to operate in what-if mode.  In this mode, code should
    ///   report what actions it would perform against a target database but
    ///   should not perform the actions.
    /// </summary>
    bool IsWhatIfMode { get; }

    /// <summary>
    ///   Gets the maximum degree of parallelism.  Always a positive integer.
    /// </summary>
    int MaxParallelism { get; }

    /// <summary>
    ///   Gets whether any seed application in the session has encountered an
    ///   error.
    /// </summary>
    bool HasErrors { get; }

    /// <summary>
    ///   Gets the console on which to report the progress of seed application
    ///   to a particular target database.
    /// </summary>
    ISeedConsole Console { get; }

    /// <summary>
    ///   Gets the token to monitor for cancellation requests.
    /// </summary>
    CancellationToken CancellationToken { get; }

    /// <summary>
    ///   Creates a log file for seed application to the specified target
    ///   database.
    /// </summary>
    /// <param name="seed">
    ///   The seed being applied.
    /// </param>
    /// <param name="target">
    ///   An object specifying how to connect to the target database.
    /// </param>
    /// <returns>
    ///   A writer that writes to the log file.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    ///   <paramref name="seed"/> and/or
    ///   <paramref name="target"/> is <see langword="null"/>.
    /// </exception>
    TextWriter CreateLog(Seed seed, SqlContextWork target);
}
