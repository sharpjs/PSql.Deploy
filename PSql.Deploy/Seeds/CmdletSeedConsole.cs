// Copyright Subatomix Research Inc.
// SPDX-License-Identifier: MIT

namespace PSql.Deploy.Seeds;

/// <summary>
///   An adapter that enables a PowerShell cmdlet to function as a user
///   interface to report the progress of content seed application.
/// </summary>
internal class CmdletSeedConsole : S.ISeedConsole
{
    private readonly ICmdlet _cmdlet;
    private readonly string? _logPath;
    private readonly object  _lock;

    /// <summary>
    ///   Initializes a new <see cref="CmdletSeedConsole"/> instance.
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
    public CmdletSeedConsole(ICmdlet cmdlet, string? logPath)
    {
        ArgumentNullException.ThrowIfNull(cmdlet);

        _cmdlet  = cmdlet;
        _logPath = logPath;
        _lock    = new();

        if (logPath is not null)
            Directory.CreateDirectory(logPath);
    }

    /// <inheritdoc/>
    public TextWriter CreateLog(S.ISeedApplication info)
    {
        if (_logPath is not { } logPath)
            return TextWriter.Null;

        var target = info.Target.FullDisplayName;
        var seed   = info.Seed.Seed.Name;

        var fileName = $"{target}.{seed}.log".SanitizeFileName();

        return TextWriter.Synchronized(
            new StreamWriter(Path.Combine(_logPath, fileName)) { AutoFlush = true }
        );
    }

    /// <inheritdoc/>
    public void ReportStarting(S.ISeedApplication info)
    {
        lock (_lock)
        {
            WriteHeader(info);

            _cmdlet.WriteHost("Starting");
        }
    }

    /// <inheritdoc/>
    public void ReportApplying(S.ISeedApplication info, string moduleName)
    {
        lock (_lock)
        {
            WriteHeader(info);

            _cmdlet.WriteHost(string.Format(
                "Applying {0}",
                moduleName
            ));
        }
    }

    /// <inheritdoc/>
    public void ReportApplied(S.ISeedApplication info, int count, TimeSpan duration, E.TargetDisposition disposition)
    {
        lock (_lock)
        {
            WriteHeader(info);

            _cmdlet.WriteHost(string.Format(
                "Applied {0} module(s) in {1:N3} second(s){2}",
                count,
                duration.TotalSeconds,
                disposition.ToMarker()
            ));
        }
    }

    /// <inheritdoc/>
    public void ReportProblem(S.ISeedApplication info, string message)
    {
        lock (_lock)
        {
            WriteHeader(info, ConsoleColor.Yellow);

            _cmdlet.WriteHost(message, foregroundColor: ConsoleColor.Yellow);
        }
    }

    private void WriteHeader(S.ISeedApplication info, ConsoleColor color = ConsoleColor.Blue)
    {
        var seed     = info.Seed.Seed.Name;
        var database = info.Target.DatabaseDisplayName;

        _cmdlet.WriteHost($"[{seed}] [{database}] ", newLine: false, color);
    }
}
