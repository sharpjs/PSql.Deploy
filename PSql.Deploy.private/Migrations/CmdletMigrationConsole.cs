// Copyright Subatomix Research Inc.
// SPDX-License-Identifier: MIT

using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace PSql.Deploy.Migrations;

internal class CmdletMigrationConsole : M.IMigrationConsole
{
    private readonly ICmdlet _cmdlet;
    private readonly string  _logPath;

    public CmdletMigrationConsole(ICmdlet cmdlet, string logPath)
    {
        if (cmdlet is null)
            throw new ArgumentNullException(nameof(cmdlet));
        if (logPath is null)
            throw new ArgumentNullException(nameof(logPath));

        _cmdlet  = cmdlet;
        _logPath = logPath;

        Directory.CreateDirectory(_logPath);
    }

    public TextWriter CreateLog(E.Target target)
    {
        var phase = M.MigrationPhase.Pre; // TODO

        var server   = target.ServerDisplayName;
        var database = target.DatabaseDisplayName;
        var fileName = SanitizeFileName($"{server}.{database}.{(int) phase}_{phase}.log");

        return new StreamWriter(Path.Combine(_logPath, fileName));
    }

    public void ReportStarting(E.Target target)
    {
        WriteHeader(target);

        _cmdlet.WriteHost("Starting");
    }

    public void ReportApplying(E.Target target, string migrationName, M.MigrationPhase phase)
    {
        WriteHeader(target);

        _cmdlet.WriteHost(string.Format(
            "Applying {0} ({1})",
            migrationName,
            phase
        ));
    }

    public void ReportApplied(E.Target target, int count, TimeSpan duration, E.TargetDisposition disposition)
    {
        WriteHeader(target);

        _cmdlet.WriteHost(string.Format(
            "Applied {0} migration(s) in {1:N3} second(s){2}",
            count,
            duration.TotalSeconds,
            E.FormattingExtensions.ToMarker(disposition) // disposition.ToMarker()
        ));
    }

    public void ReportProblem(E.Target? target, string message)
    {
        if (target is not null)
            WriteHeader(target, ConsoleColor.Yellow);

        _cmdlet.WriteHost(message, foregroundColor: ConsoleColor.Yellow);
    }

    private void WriteHeader(E.Target target, ConsoleColor color = ConsoleColor.Blue)
    {
        _cmdlet.WriteHost($"[{target.DatabaseDisplayName}] ", newLine: false, color);
    }

    [return: NotNullIfNotNull(nameof(value))]
    internal static string? SanitizeFileName(string? value)
    {
        if (value is null)
            return null;

        var invalid = Path.GetInvalidFileNameChars();
        var index   = value.IndexOfAny(invalid);
        if (index < 0)
            return value;

        var start  = 0;
        var result = new StringBuilder(value.Length);

        do
        {
            result.Append(value, start, index - start).Append('_');

            start = index + 1;
            index = value.IndexOfAny(invalid, start);
        }
        while (index >= 0);

        return result.Append(value, start, value.Length - start).ToString();
    }
}
