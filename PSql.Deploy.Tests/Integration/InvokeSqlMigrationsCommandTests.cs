// Copyright Subatomix Research Inc.
// SPDX-License-Identifier: MIT

using PSql.Deploy.Migrations;

namespace PSql.Deploy.Integration;

[TestFixture]
[Parallelizable(ParallelScope.Fixtures)] // because tests write to the same files
public class InvokeSqlMigrationsCommandIntegrationTests
{
    [Test]
    public void Invoke()
    {
        File.Delete("..PSqlDeployTestA.0_Pre.log");
        File.Delete("..PSqlDeployTestA.1_Core.log");
        File.Delete("..PSqlDeployTestA.2_Post.log");
        File.Delete("..PSqlDeployTestB.0_Pre.log");
        File.Delete("..PSqlDeployTestB.1_Core.log");
        File.Delete("..PSqlDeployTestB.2_Post.log");

        var (output, exception) = ScriptExecutor.Execute(
            """
            $TargetA = New-SqlContext -DatabaseName PSqlDeployTestA
            $TargetB = New-SqlContext -DatabaseName PSqlDeployTestB
            $Targets = New-SqlTargetDatabaseGroup -Target $TargetA, $TargetB -Name Test
            $Path    = Join-Path TestDbs A

            # Test explicit -Phase
            Invoke-SqlMigrations    `
                -Target   $Targets  `
                -Path     $Path     `
                -Phase    Pre, Core `
                -Confirm: $false

            Get-SqlMigrations -Target $TargetA

            # Test implicit -Phase and continuing a partial application
            Invoke-SqlMigrations   `
                -Target   $Targets `
                -Path     $Path    `
                -Confirm: $false

            Get-SqlMigrations -Target $TargetA
            """
        );

        exception.ShouldBeNull();

        output.Count.ShouldBeGreaterThanOrEqualTo(6);

        var migrations = output.Select(o => o?.BaseObject).OfType<Migration>().ToList();

        migrations.Count.ShouldBe(6);

        // From Pre+Core run
        migrations[0].Name .ShouldBe("Migration0");
        migrations[0].Hash .ShouldBe("D8462C316FD72659FB11FA7C9727D05707F8332B");
        migrations[0].State.ShouldBe(MigrationState.AppliedCore);

        migrations[1].Name .ShouldBe("Migration1");
        migrations[1].Hash .ShouldBe("2909F7C67C9B831FFCD4655F31683941F700A205");
        migrations[1].State.ShouldBe(MigrationState.AppliedCore);

        migrations[2].Name .ShouldBe("Migration2");
        migrations[2].Hash .ShouldBe("FB049E6EA9DC10019088850C94E9C5D2661A6DE7");
        migrations[2].State.ShouldBe(MigrationState.AppliedCore);

        // From Post run
        migrations[3].Name .ShouldBe("Migration0");
        migrations[3].Hash .ShouldBe("D8462C316FD72659FB11FA7C9727D05707F8332B");
        migrations[3].State.ShouldBe(MigrationState.AppliedPost);

        migrations[4].Name .ShouldBe("Migration1");
        migrations[4].Hash .ShouldBe("2909F7C67C9B831FFCD4655F31683941F700A205");
        migrations[4].State.ShouldBe(MigrationState.AppliedPost);

        migrations[5].Name .ShouldBe("Migration2");
        migrations[5].Hash .ShouldBe("FB049E6EA9DC10019088850C94E9C5D2661A6DE7");
        migrations[5].State.ShouldBe(MigrationState.AppliedPost);

        File.ReadAllText("..PSqlDeployTestA.0_Pre.log" ).ShouldNotBeNullOrEmpty();
        File.ReadAllText("..PSqlDeployTestA.1_Core.log").ShouldNotBeNullOrEmpty();
        File.ReadAllText("..PSqlDeployTestA.2_Post.log").ShouldNotBeNullOrEmpty();
        File.ReadAllText("..PSqlDeployTestB.0_Pre.log" ).ShouldNotBeNullOrEmpty();
        File.ReadAllText("..PSqlDeployTestB.1_Core.log").ShouldNotBeNullOrEmpty();
        File.ReadAllText("..PSqlDeployTestB.2_Post.log").ShouldNotBeNullOrEmpty();
    }

    [Test]
    public void Invoke_DefaultPath()
    {
        var (_, exception) = ScriptExecutor.Execute(
            """
            Join-Path TestDbs A | Set-Location
            $Target = New-SqlContext -DatabaseName PSqlDeployTestA
            Invoke-SqlMigrations $Target -WhatIf
            """
        );

        exception.ShouldBeNull();
    }
}
