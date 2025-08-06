// Copyright Subatomix Research Inc.
// SPDX-License-Identifier: MIT

namespace PSql.Deploy.Commands;

// Resolve ambiguity
using AllowNullAttribute = System.Management.Automation.AllowNullAttribute;

[Cmdlet(VerbsDiagnostic.Test, "CmdletExtensions")]
public class TestCmdletExtensionsCommand : PSCmdlet
{
    public enum TestCase 
    {
        IsWhatIf,
        GetCurrentPath,
        WriteHost,
    }

    [Parameter(Mandatory = true, Position = 0)]
    public TestCase Case { get; set; }

    [Parameter]
    [AllowNull]
    public object? WhatIf { get; set; }
    //     ^^^^^^^
    // Intentionally object? instead of SwitchParameter to test all code paths.

    [Parameter]
    public string? Message { get; set; }

    protected override void ProcessRecord()
    {
        switch (Case)
        {
            case TestCase.IsWhatIf:
                WriteObject(this.IsWhatIf());
                break;

            case TestCase.GetCurrentPath:
                WriteObject(this.GetCurrentPath());
                break;

            default: // case TestCase.WriteHost:
                this.WriteHost(Message);
                break;
        }
    }
}
