// Copyright Subatomix Research Inc.
// SPDX-License-Identifier: MIT

namespace PSql.Deploy;

internal class CmdletSqlMessageLogger : E.ISqlMessageLogger
{
    const int MaxInformationalSeverity = 10;

    private readonly ICmdlet _cmdlet;

    public CmdletSqlMessageLogger(ICmdlet cmdlet)
    {
        if (cmdlet is null)
            throw new ArgumentNullException(nameof(cmdlet));

        _cmdlet = cmdlet;
    }

    public void Log(string procedure, int line, int number, int severity, string message)
    {
        if (severity <= MaxInformationalSeverity)
            _cmdlet.WriteHost(message);
        else
            _cmdlet.WriteWarning($"{procedure}:{line}: E{number}:{severity}: {message}");
    }
}
