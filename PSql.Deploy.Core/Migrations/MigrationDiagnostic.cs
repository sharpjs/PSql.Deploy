// Copyright Subatomix Research Inc.
// SPDX-License-Identifier: MIT

namespace PSql.Deploy.Migrations;

/// <summary>
///   A diagnostic message for a migration.
/// </summary>
internal class MigrationDiagnostic
{
    /// <summary>
    ///   Initializes a new <see cref="MigrationDiagnostic"/> instance.
    /// </summary>
    /// <param name="isError">
    ///   <see langword="true"/> if the diagnostic message is an error;
    ///   <see langword="false"/> otherwise.
    /// </param>
    /// <param name="message">
    ///   The text content of the diagnostic message.
    /// </param>
    /// <exception cref="ArgumentNullException">
    ///   <paramref name="message"/> is <see langword="null"/>.
    /// </exception>
    public MigrationDiagnostic(bool isError, string message)
    {
        if (message is null)
            throw new ArgumentNullException(nameof(message));

        IsError = isError;
        Message = message;
    }

    /// <summary>
    ///   Gets whether the diagnostic message is an error.
    /// </summary>
    public bool IsError { get; }

    /// <summary>
    ///   Gets the content of the diagnostic message.
    /// </summary>
    public string Message { get; }
}
