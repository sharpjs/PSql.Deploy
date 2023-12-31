// Copyright 2023 Subatomix Research Inc.
// SPDX-License-Identifier: ISC

namespace PSql.Deploy.Seeding;

/// <summary>
///   A <see cref="ISqlMessageLogger"/> implementation to log content seed
///   application.
/// </summary>
internal sealed class SeedSqlMessageLogger : ISqlMessageLogger
{
    private readonly TextWriter _writer;
    private readonly int        _workerId;

    /// <summary>
    ///   Initializes a new <see cref="SeedSqlMessageLogger"/> instance with
    ///   the specified writer and worker identifier.
    /// </summary>
    /// <param name="writer">
    ///   The writer to use to write messages.
    /// </param>
    /// <param name="workerId">
    ///   The unique identifier of the worker.
    /// </param>
    /// <exception cref="ArgumentNullException">
    ///   <paramref name="writer"/> is <see langword="null"/>.
    /// </exception>
    public SeedSqlMessageLogger(TextWriter writer, int workerId)
    {
        if (writer is null)
            throw new ArgumentNullException(nameof(writer));

        _writer   = writer;
        _workerId = workerId;
    }

    /// <inheritdoc/>
    public void LogInformation(string message)
        => _writer.WriteLine($"{_workerId}> {message}");

    /// <inheritdoc/>
    public void LogError(string message)
        => _writer.WriteLine($"{_workerId}> WARNING: {message}");
}
