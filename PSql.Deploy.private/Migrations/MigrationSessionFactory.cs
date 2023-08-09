// Copyright 2023 Subatomix Research Inc.
// SPDX-License-Identifier: ISC

namespace PSql.Deploy.Migrations;

public static class MigrationSessionFactory
{
    /// <summary>
    ///   Creates a new <see cref="IMigrationSessionControl"/> instance.
    /// </summary>
    /// <param name="logPath">
    ///   The path of a directory in which to save per-database log files.
    /// </param>
    /// <param name="cancellation">
    ///   The token to monitor for cancellation requests.
    /// </param>
    /// <exception cref="ArgumentNullException">
    ///   <paramref name="logPath"/> is <see langword="null"/>.
    /// </exception>
    public static IMigrationSessionControl Create(string logPath, CancellationToken cancellation)
        => new MigrationSession(logPath, cancellation);
}
