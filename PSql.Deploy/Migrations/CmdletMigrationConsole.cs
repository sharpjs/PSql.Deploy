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
    public CmdletMigrationConsole(ICmdlet cmdlet, string? logPath)
    {
        ArgumentNullException.ThrowIfNull(cmdlet);

        _cmdlet  = cmdlet;
        _logPath = logPath;
        _lock    = new();

        if (logPath is not null)
            Directory.CreateDirectory(logPath);
    }

    /// <inheritdoc/>
    public TextWriter CreateLog(M.IMigrationApplication info)
    {
        if (_logPath is not { } logPath)
            return TextWriter.Null;

        var phase    = info.Session.CurrentPhase;
        var server   = info.Target.ServerDisplayName;
        var database = info.Target.DatabaseDisplayName;
        var fileName = $"{server}.{database}.{(int) phase}_{phase}.log".SanitizeFileName();

        return new StreamWriter(Path.Combine(logPath, fileName)) { AutoFlush = true };
    }

    /// <inheritdoc/>
    public void ReportStarting(M.IMigrationApplication info)
    {
        lock (_lock)
        {
            WriteHeader(info);

            _cmdlet.WriteHost("Starting");
        }
    }

    /// <inheritdoc/>
    public void ReportApplying(M.IMigrationApplication info,
        string migrationName, M.MigrationPhase phase)
    {
        lock (_lock)
        {
            WriteHeader(info);

            _cmdlet.WriteHost(string.Format(
                "Applying {0} ({1})",
                migrationName,
                phase
            ));
        }
    }

    /// <inheritdoc/>
    public void ReportApplied(M.IMigrationApplication info,
        int count, TimeSpan duration, E.TargetDisposition disposition)
    {
        lock (_lock)
        {
            WriteHeader(info);

            _cmdlet.WriteHost(string.Format(
                "Applied {0} migration(s) in {1:N3} second(s){2}",
                count,
                duration.TotalSeconds,
                disposition.ToMarker()
            ));
        }
    }

    /// <inheritdoc/>
    public void ReportProblem(M.IMigrationApplication info, string message)
    {
        lock (_lock)
        {
            WriteHeader(info, ConsoleColor.Yellow);

            _cmdlet.WriteHost(message, foregroundColor: ConsoleColor.Yellow);
        }
    }

    private void WriteHeader(M.IMigrationApplication info, ConsoleColor color = ConsoleColor.Blue)
    {
        var phase    = info.Session.CurrentPhase;
        var database = info.Target.DatabaseDisplayName;

        _cmdlet.WriteHost($"[{phase}] [{database}] ", newLine: false, color);
    }
}
