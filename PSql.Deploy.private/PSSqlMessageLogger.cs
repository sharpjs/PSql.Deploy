// Copyright Subatomix Research Inc.
// SPDX-License-Identifier: MIT

extern alias Engine;

namespace PSql.Deploy;

using D = Engine::PSql.Deploy;

internal class PSSqlMessageLogger : D.ISqlMessageLogger
{
    const int MaxInformationalSeverity = 10;

    private readonly ICmdlet _cmdlet;

    public PSSqlMessageLogger(ICmdlet cmdlet)
    {
        if (cmdlet is null)
            throw new ArgumentNullException(nameof(cmdlet));

        _cmdlet = cmdlet;
    }

    /// <inheritdoc/>
    public void Log(string procedure, int line, int number, int severity, string message)
    {
        if (severity <= MaxInformationalSeverity)
            _cmdlet.WriteHost(message);
        else
            _cmdlet.WriteWarning($"{procedure}:{line}: E{number}:L{severity}: {message}");
    }
}
