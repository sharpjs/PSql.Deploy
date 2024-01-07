// Copyright 2024 Subatomix Research Inc.
// SPDX-License-Identifier: ISC

namespace PSql.Deploy.Migrations;

/// <summary>
///   A factory for migration sessions.
/// </summary>
public static class MigrationSessionFactory
{
    /// <summary>
    ///   Creates a new <see cref="IMigrationSessionControl"/> instance.
    /// </summary>
    /// <param name="console">
    ///   The console on which to report the progress of migration application
    ///   to a particular target database.
    /// </param>
    /// <param name="logPath">
    ///   The path of a directory in which to save per-database log files.
    /// </param>
    /// <param name="cancellation">
    ///   The token to monitor for cancellation requests.
    /// </param>
    /// <exception cref="ArgumentNullException">
    ///   <paramref name="console"/> and/or
    ///   <paramref name="logPath"/> is <see langword="null"/>.
    /// </exception>
    public static IMigrationSessionControl Create(
        IMigrationConsole console,
        string            logPath,
        CancellationToken cancellation)
    {
        return new MigrationSession(console, logPath, cancellation);
    }
}
