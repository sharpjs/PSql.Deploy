// Copyright 2023 Subatomix Research Inc.
// SPDX-License-Identifier: ISC

namespace PSql.Deploy.Migrations;

/// <summary>
///   An object that logs messages produced during a migration run.
/// </summary>
public interface IMigrationLogger : ISqlMessageLogger
{
    /// <summary>
    ///   Logs the specified message relating to a migration run.
    /// </summary>
    /// <param name="message">
    ///   A message relating to a migration run.
    /// </param>
    void Log(MigrationMessage message);
}
