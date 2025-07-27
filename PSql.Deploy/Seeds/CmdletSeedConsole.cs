// Copyright Subatomix Research Inc.
// SPDX-License-Identifier: MIT

namespace PSql.Deploy.Seeds;

internal class CmdletSeedConsole : S.ISeedConsole
{
    private readonly ICmdlet _cmdlet;
    private readonly string  _logPath;

    public CmdletSeedConsole(ICmdlet cmdlet, string logPath)
    {
        if (cmdlet is null)
            throw new ArgumentNullException(nameof(cmdlet));
        if (logPath is null)
            throw new ArgumentNullException(nameof(logPath));

        _cmdlet  = cmdlet;
        _logPath = logPath;

        Directory.CreateDirectory(_logPath);
    }

    public TextWriter CreateLog(S.ISeedApplication info)
    {
        var target = info.Target.FullDisplayName;
        var seed   = info.Seed.Seed.Name;

        var fileName = $"{target}.{seed}.log".SanitizeFileName();

        return new StreamWriter(Path.Combine(_logPath, fileName));
    }

    public void ReportApplied(S.ISeedApplication info, int count, TimeSpan duration, E.TargetDisposition disposition)
    {
    }

    public void ReportApplying(S.ISeedApplication info, string moduleName)
    {
    }

    public void ReportProblem(S.ISeedApplication info, string message)
    {
    }

    public void ReportStarting(S.ISeedApplication info)
    {
    }
}
