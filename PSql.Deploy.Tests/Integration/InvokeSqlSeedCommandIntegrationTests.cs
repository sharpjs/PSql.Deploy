// Copyright Subatomix Research Inc.
// SPDX-License-Identifier: MIT

using System.Management.Automation.Runspaces;

namespace PSql.Deploy.Integration;

[TestFixture]
[Parallelizable(ParallelScope.Fixtures)] // because tests write to the same files
public class InvokeSqlSeedCommandIntegrationTests
{
    [Test]
    public void Invoke()
    {
        File.Delete("..PSqlDeployTestA.Typical.log");
        File.Delete("..PSqlDeployTestB.Typical.log");

        var (_, exception) = ScriptExecutor.Execute(
            """
            $TargetA = New-SqlContext -DatabaseName PSqlDeployTestA
            $TargetB = New-SqlContext -DatabaseName PSqlDeployTestB
            $Targets = New-SqlTargetDatabaseGroup -Target $TargetA, $TargetB -Name Test
            $Path    = Join-Path TestDbs A -Resolve

            Invoke-SqlSeed                  `
                -Target   $Targets          `
                -Path     $Path             `
                -Seed     Typical           `
                -Define   @{ foo = "bar" }  `
                -Confirm: $false            # because tests run in non-interactive mode
            """
        );

        exception.ShouldBeNull();

        File.ReadAllText("..PSqlDeployTestA.Typical.log").ShouldNotBeNullOrEmpty();
        File.ReadAllText("..PSqlDeployTestB.Typical.log").ShouldNotBeNullOrEmpty();
    }

    [Test]
    public void Invoke_DefaultPath()
    {
        var (_, exception) = ScriptExecutor.Execute(
            """
            Join-Path TestDbs A | Set-Location
            $Target = New-SqlContext -DatabaseName PSqlDeployTestA

            Invoke-SqlSeed $Target Typical -Define @{ foo = "bar" } -WhatIf
            """
        );

        exception.ShouldBeNull();
    }

    [Test]
    public void Invoke_DefineMissing()
    {
        // NOTE: The test seed 'Typical' requires a SqlCmd variable 'foo'.

        var (_, exception) = ScriptExecutor.Execute(
            """
            Join-Path TestDbs A | Set-Location
            $Target = New-SqlContext -DatabaseName PSqlDeployTestA

            Invoke-SqlSeed $Target Typical <#-Define#> -WhatIf
            """
        );

        exception.ShouldBeOfType<S.SeedException>()
            .InnerException.ShouldNotBeNull()
            .Message.ShouldBe("SqlCmd variable 'foo' is not defined.");
    }

    [Test]
    public void Invoke_DefineEmpty()
    {
        // NOTE: The test seed 'Typical' requires a SqlCmd variable 'foo'.

        var (_, exception) = ScriptExecutor.Execute(
            """
            Join-Path TestDbs A | Set-Location
            $Target = New-SqlContext -DatabaseName PSqlDeployTestA

            Invoke-SqlSeed $Target Typical -Define @{} -WhatIf
            """
        );

        exception.ShouldBeOfType<S.SeedException>()
            .InnerException.ShouldNotBeNull()
            .Message.ShouldBe("SqlCmd variable 'foo' is not defined.");
    }

    [Test]
    public void Invoke_DefineWithNullKey()
    {
        var variable = new SessionStateVariableEntry(
            name:        nameof(ToStringIsNull),
            value:       new ToStringIsNull(),
            description: "An object with a ToString() method that returns null."
        );

        var (_, exception) = ScriptExecutor.Execute(
            state => state.Variables.Add(variable),
            """
            Join-Path TestDbs A | Set-Location
            $Target = New-SqlContext -DatabaseName PSqlDeployTestA

            Invoke-SqlSeed $Target Typical -Define @{ $ToStringIsNull = "value" } -WhatIf
            """
        );

        exception.ShouldBeOfType<ArgumentException>().Message.ShouldBe(
            "Key must be non-null and must convert to a non-empty string. " +
            "(Parameter 'Define')"
        );
    }

    [Test]
    public void Invoke_DefineWithNullValue()
    {
        var (_, exception) = ScriptExecutor.Execute(
            """
            Join-Path TestDbs A | Set-Location
            $Target = New-SqlContext -DatabaseName PSqlDeployTestA

            Invoke-SqlSeed $Target Typical -Define @{ foo = $null } -WhatIf
            """
        );

        exception.ShouldBeNull();
    }
}
