// Copyright 2024 Subatomix Research Inc.
// SPDX-License-Identifier: ISC

namespace PSql.Deploy;

internal static class CmdletExtensions
{
    public static bool IsWhatIf(this PSCmdlet cmdlet)
    {
        if (cmdlet is null)
            throw new ArgumentNullException(nameof(cmdlet));

        return cmdlet.GetVariableValue("WhatIfPreference") is not null or false;
    }

    public static string GetCurrentPath(this PSCmdlet cmdlet)
    {
        if (cmdlet is null)
            throw new ArgumentNullException(nameof(cmdlet));

        return cmdlet.SessionState.Path.CurrentFileSystemLocation.Path;
    }
}
