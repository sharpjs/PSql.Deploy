// Copyright 2023 Subatomix Research Inc.
// SPDX-License-Identifier: ISC

using System.Diagnostics;

namespace PSql.Deploy.Migrations;

/// <summary>
///   Represents a target database.
/// </summary>
internal sealed class MigrationTarget : IDisposable
{
    /// <summary>
    ///   Initializes a new <see cref="MigrationTarget"/> instance.
    /// </summary>
    /// <param name="context">
    ///   An object specifying how to connect to the target database.
    /// </param>
    /// <param name="phase">
    ///   The migration phase being applied.
    /// </param>
    /// <param name="logPath">
    ///   The path of a directory in which to save per-database log files.
    /// </param>
    /// <exception cref="ArgumentNullException"></exception>
    public MigrationTarget(SqlContext context, MigrationPhase phase, string logPath)
    {
        if (context is null)
            throw new ArgumentNullException(nameof(context));
        if (logPath is null)
            throw new ArgumentNullException(nameof(logPath));

        _stopwatch   = Stopwatch.StartNew();
        Context      = context;
        ServerName   = context.AsAzure?.ServerResourceName ?? context.ServerName ?? "local";
        DatabaseName = context.DatabaseName ?? "default";
        LogFileName  = $"{ServerName}.{DatabaseName}.{phase}.log".SanitizeFileName();
        LogWriter    = new StreamWriter(Path.Combine(logPath, LogFileName));
        LogConsole   = new TextWriterConsole(LogWriter);
    }

    private readonly Stopwatch _stopwatch;

    /// <summary>
    ///   Gets an object that specifies how to connect to the target database.
    /// </summary>
    public SqlContext Context { get; }

    /// <summary>
    ///   Gets a display name for the database server.  This name might be a
    ///   DNS name, an Azure resource name, or a placeholder indicating a local
    ///   SQL Server instance.
    /// </summary>
    public string ServerName { get; }

    /// <summary>
    ///   Gets a short name for the database.  This name might be a real
    ///   database name or a placeholder indicating the default database for
    ///   the connection.
    /// </summary>
    public string DatabaseName { get; }

    /// <summary>
    ///   Gets the name of the per-database log file.
    /// </summary>
    public string LogFileName { get; }

    /// <summary>
    ///   Gets a writer that writes to the per-database log file.
    /// </summary>
    public TextWriter LogWriter { get; }

    /// <summary>
    ///   Gets an <see cref="IConsole"/> implementation that writes to the
    ///   per-database log file.
    /// </summary>
    public IConsole LogConsole { get; }

    /// <summary>
    ///   Gets the time that has elapsed since construction of this object.
    /// </summary>
    public TimeSpan ElapsedTime => _stopwatch.Elapsed;

    /// <summary>
    ///   Opens a connection to the target database.  The connection will log
    ///   server messages to the per-database log file.
    /// </summary>
    /// <returns>
    ///   An open connection to the target database.
    /// </returns>
    public ISqlConnection Connect()
        => Context.Connect(databaseName: null, LogConsole);

    /// <summary>
    ///   Writes the specified text and a line ending to the per-database log
    ///   file.
    /// </summary>
    /// <param name="text">
    ///   The text to write.
    /// </param>
    public void Log(string text)
        => LogWriter.WriteLine(text);

    /// <inheritdoc/>
    public void Dispose()
        => LogWriter.Dispose();
}
