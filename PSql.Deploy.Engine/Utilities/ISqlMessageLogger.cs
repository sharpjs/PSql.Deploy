// Copyright Subatomix Research Inc.
// SPDX-License-Identifier: MIT

namespace PSql.Deploy;

/// <summary>
///   An object that logs server messages received over a database connection.
/// </summary>
internal interface ISqlMessageLogger
{
    // DESIGN: Do not expose SqlClient types publicly.

    /// <summary>
    ///   Logs the specified message.
    /// </summary>
    /// <param name="message">
    ///   The message to log.
    /// </param>
    void Log(SqlError message);
    //          ^^^^^
    // Despite the name, non-error messages also arrive this way.

    void Log(string? procedure, int line, int number, int severity, string message);
}
