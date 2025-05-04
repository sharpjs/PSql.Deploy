// Copyright Subatomix Research Inc.
// SPDX-License-Identifier: MIT

namespace PSql.Deploy.Seeding;

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

    public TextWriter CreateLog(E.Target target, S.Seed seed)
    {
        var fileName = $"{target.FullDisplayName}.{seed.Name}.log".SanitizeFileName();

        return new StreamWriter(Path.Combine(_logPath, fileName));
    }

    public void ReportApplied(E.Target target, int count, TimeSpan duration, E.TargetDisposition disposition)
    {
    }

    public void ReportApplying(E.Target target, string moduleName)
    {
    }

    public void ReportProblem(E.Target? target, string message)
    {
    }

    public void ReportStarting(E.Target target)
    {
    }
}
