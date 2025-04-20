// Copyright Subatomix Research Inc.
// SPDX-License-Identifier: MIT

namespace PSql.Deploy;

/// <summary>
///   An <see cref="ISqlMessageLogger"/> implementation that writes messages to
///   a <see cref="TextWriter"/>.
/// </summary>
internal sealed class TextWriterSqlMessageLogger : ISqlMessageLogger
{
    private readonly TextWriter _writer;

    /// <summary>
    ///   Initializes a new <see cref="TextWriterSqlMessageLogger"/> instance
    ///   with the specified writer.
    /// </summary>
    /// <param name="writer">
    ///   The writer to use to write messages.
    /// </param>
    /// <exception cref="ArgumentNullException">
    ///   <paramref name="writer"/> is <see langword="null"/>.
    /// </exception>
    public TextWriterSqlMessageLogger(TextWriter writer)
    {
        if (writer is null)
            throw new ArgumentNullException(nameof(writer));

        _writer = writer;
    }

    /// <inheritdoc/>
    public void LogInformation(string message)
        => _writer.WriteLine(message);

    /// <inheritdoc/>
    public void LogError(string message)
        => _writer.WriteLine(string.Concat("WARNING: ", message));

    // TODO
    public void Log(SqlError message) => throw new NotImplementedException();
    public void Log(string? procedure, int line, int number, int severity, string message) => throw new NotImplementedException();
}
