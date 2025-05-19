// Copyright Subatomix Research Inc.
// SPDX-License-Identifier: MIT

namespace PSql.Deploy.Migrations;

/// <summary>
///   An adapter that enables a PowerShell cmdlet to function as a user
///   interface to report the progress of schema migration application.
/// </summary>
internal class CmdletMigrationConsole : M.IMigrationConsole
{
    private readonly ICmdlet _cmdlet;
    private readonly string? _logPath;
    private readonly object  _lock;

    /// <summary>
    ///   Initializes a new <see cref="CmdletMigrationConsole"/> instance.
    /// </summary>
    /// <param name="cmdlet">
    ///   The PowerShell cmdlet to adapt.
    /// </param>
    /// <param name="logPath">
    ///   The path of directory in which to store per-phase, per-target log
    ///   files, or <see langword="null"/> to disable logging.
    /// </param>
    /// <exception cref="ArgumentNullException">
    ///   <paramref name="cmdlet"/>  is <see langword="null"/>.
    /// </exception>
    internal CmdletMigrationConsole(ICmdlet cmdlet, string? logPath)
    {
        if (cmdlet is null)
            throw new ArgumentNullException(nameof(cmdlet));

        _cmdlet  = cmdlet;
        _logPath = logPath;
        _lock    = new();

        if (logPath is not null)
            Directory.CreateDirectory(logPath);
    }

    /// <inheritdoc/>
    public TextWriter CreateLog(M.IMigrationSession session, E.Target target)
    {
        if (_logPath is not { } logPath)
            return TextWriter.Null;

        var phase    = session.CurrentPhase;
        var server   = target.ServerDisplayName;
        var database = target.DatabaseDisplayName;
        var fileName = $"{server}.{database}.{(int) phase}_{phase}.log".SanitizeFileName();

        return new StreamWriter(Path.Combine(logPath, fileName));
    }

    /// <inheritdoc/>
    public void ReportStarting(M.IMigrationSession session, E.Target target)
    {
        lock (_lock)
        {
            WriteHeader(session, target);

            _cmdlet.WriteHost("Starting");
        }
    }

    /// <inheritdoc/>
    public void ReportApplying(M.IMigrationSession session, E.Target target,
        string migrationName, M.MigrationPhase phase)
    {
        lock (_lock)
        {
            WriteHeader(session, target);

            _cmdlet.WriteHost(string.Format(
                "Applying {0} ({1})",
                migrationName,
                phase
            ));
        }
    }

    /// <inheritdoc/>
    public void ReportApplied(M.IMigrationSession session, E.Target target,
        int count, TimeSpan duration, E.TargetDisposition disposition)
    {
        lock (_lock)
        {
            WriteHeader(session, target);

            _cmdlet.WriteHost(string.Format(
                "Applied {0} migration(s) in {1:N3} second(s){2}",
                count,
                duration.TotalSeconds,
                disposition.ToMarker()
            ));
        }
    }

    /// <inheritdoc/>
    public void ReportProblem(M.IMigrationSession session, E.Target? target, string message)
    {
        lock (_lock)
        {
            WriteHeader(session, target, ConsoleColor.Yellow);

            _cmdlet.WriteHost(message, foregroundColor: ConsoleColor.Yellow);
        }
    }

    private void WriteHeader(
        M.IMigrationSession session,
        E.Target?           target = null,
        ConsoleColor        color  = ConsoleColor.Blue)
    {
        var message = target is null
            ? $"[{session.CurrentPhase}] "
            : $"[{session.CurrentPhase}] [{target.DatabaseDisplayName}] ";

        _cmdlet.WriteHost(message, newLine: false, color);
    }
}
