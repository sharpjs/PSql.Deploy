// Copyright Subatomix Research Inc.
// SPDX-License-Identifier: MIT

namespace PSql.Deploy;

using static SqlMessageConstants;

/// <summary>
///   An <see cref="ISqlMessageLogger"/> implementation that writes messages to
///   a <see cref="TextWriter"/> with a prefix.
/// </summary>
public sealed class PrefixTextWriterSqlMessageLogger : ISqlMessageLogger
{
    private readonly TextWriter _writer;
    private readonly string     _prefix;

    /// <summary>
    ///   Initializes a new <see cref="PrefixTextWriterSqlMessageLogger"/>
    ///   instance with the specified writer and prefix.
    /// </summary>
    /// <param name="writer">
    ///   The writer to use to write messages.
    /// </param>
    /// <param name="prefix">
    ///   The prefix to prepend to each message.
    /// </param>
    /// <exception cref="ArgumentNullException">
    ///   <paramref name="writer"/> is <see langword="null"/>.
    /// </exception>
    public PrefixTextWriterSqlMessageLogger(TextWriter writer, string prefix)
    {
        ArgumentNullException.ThrowIfNull(writer);
        ArgumentNullException.ThrowIfNull(prefix);

        _writer = writer;
        _prefix = prefix;
    }

    /// <inheritdoc/>
    public void Log(string procedure, int line, int number, int severity, string? message)
    {
        if (severity <= MaxInformationalSeverity)
            _writer.WriteLine($"{_prefix} {message}");
        else
            _writer.WriteLine($"{_prefix} {procedure}:{line}: E{number}:{severity}: {message}");
    }
}
