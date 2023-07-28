// Copyright 2023 Subatomix Research Inc.
// SPDX-License-Identifier: ISC

namespace PSql.Deploy.Migrations;

public static class MigrationEngineFactory
{
    /// <summary>
    ///   Creates a new <see cref="IMigrationEngine"/> instance.
    /// </summary>
    /// <param name="console">
    ///   The console on which to display status and important messages.
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
    public static IMigrationEngine Create(
        IConsole          console,
        string            logPath,
        CancellationToken cancellation)
        => new MigrationEngine(console, logPath, cancellation);
}
