// Copyright Subatomix Research Inc.
// SPDX-License-Identifier: ISC

using System.Management.Automation.Language;

namespace PSql.Deploy;

internal static class ScriptBlockExtensions
{
    public static ScriptBlock Clone(this ScriptBlock scriptBlock)
    {
        if (scriptBlock is null)
            throw new ArgumentNullException(nameof(scriptBlock));

        return ((ScriptBlockAst) scriptBlock.Ast).GetScriptBlock();
    }
}
