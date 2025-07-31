// Copyright Subatomix Research Inc.
// SPDX-License-Identifier: MIT

namespace PSql.Deploy.Integration;

using static ScriptExecutor;

[TestFixture]
public class IntegrationTests
{
    [Test]
    public void IntegrationTest0()
    {
        File.Delete("..PSqlDeployTestA.0_Pre.log");
        File.Delete("..PSqlDeployTestA.1_Core.log");
        File.Delete("..PSqlDeployTestA.2_Post.log");
        File.Delete("..PSqlDeployTestA.Typical.log");
        File.Delete("..PSqlDeployTestB.0_Pre.log");
        File.Delete("..PSqlDeployTestB.1_Core.log");
        File.Delete("..PSqlDeployTestB.2_Post.log");
        File.Delete("..PSqlDeployTestB.Typical.log");

        var (output, exception) = Execute(
            $$"""
            Import-Module PSql

            $TargetA = New-SqlContext -DatabaseName PSqlDeployTestA
            $TargetB = New-SqlContext -DatabaseName PSqlDeployTestB
            $Targets = New-SqlTargetDatabaseGroup -Target $TargetA, $TargetB -Name Test
            $Path    = Join-Path TestDbs A -Resolve

            Invoke-SqlMigrations   `
                -Target   $Targets `
                -Path     $Path    `
                -WhatIf

            Invoke-SqlMigrations   `
                -Target   $Targets `
                -Path     $Path    `
                -Confirm: $false

            Invoke-SqlSeed                 `
                -Target   $Targets         `
                -Path     $Path            `
                -Seed     Typical          `
                -Define   @{ foo = "bar" } `
                -WhatIf

            Invoke-SqlSeed                 `
                -Target   $Targets         `
                -Path     $Path            `
                -Seed     Typical          `
                -Define   @{ foo = "bar" } `
                -Confirm: $false

            $DefinedMigrations = Get-SqlMigrations -Path   $Path
            $AppliedMigrations = Get-SqlMigrations -Target $TargetA
            """
        );

        exception.ShouldBeNull();
        output   .ShouldBeEmpty();
    }
}
