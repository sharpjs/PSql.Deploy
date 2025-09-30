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

        var (output, exception) = Execute(
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

            # Test pipeline input
            $TargetA | Get-SqlMigrations
            [PSql.Deploy.SqlTargetDatabase]::new($TargetA) | Get-SqlMigrations
            """
        );

        exception.ShouldBeNull();

        output.Count.ShouldBeGreaterThanOrEqualTo(6);

        var migrations = output.Select(o => o?.BaseObject).OfType<Migration>().ToList();

        migrations.Count.ShouldBe(9);

        void ShouldBeMigrations(int n, MigrationState state)
        {
            migrations[n + 0].Name .ShouldBe("Migration0");
            migrations[n + 0].Hash .ShouldBe("D8462C316FD72659FB11FA7C9727D05707F8332B");
            migrations[n + 0].State.ShouldBe(state);

            migrations[n + 1].Name .ShouldBe("Migration1");
            migrations[n + 1].Hash .ShouldBe("2909F7C67C9B831FFCD4655F31683941F700A205");
            migrations[n + 1].State.ShouldBe(state);

            migrations[n + 2].Name .ShouldBe("Migration2");
            migrations[n + 2].Hash .ShouldBe("FB049E6EA9DC10019088850C94E9C5D2661A6DE7");
            migrations[n + 2].State.ShouldBe(state);
        }

        // From Pre+Core run
        ShouldBeMigrations(0, MigrationState.AppliedCore);

        // From Post run
        ShouldBeMigrations(3, MigrationState.AppliedPost);
        ShouldBeMigrations(6, MigrationState.AppliedPost);

        File.ReadAllText("local.PSqlDeployTestA.0_Pre.log" ).ShouldNotBeNullOrEmpty();
        File.ReadAllText("local.PSqlDeployTestA.1_Core.log").ShouldNotBeNullOrEmpty();
        File.ReadAllText("local.PSqlDeployTestA.2_Post.log").ShouldNotBeNullOrEmpty();
        File.ReadAllText("local.PSqlDeployTestB.0_Pre.log" ).ShouldNotBeNullOrEmpty();
        File.ReadAllText("local.PSqlDeployTestB.1_Core.log").ShouldNotBeNullOrEmpty();
        File.ReadAllText("local.PSqlDeployTestB.2_Post.log").ShouldNotBeNullOrEmpty();
    }

    [Test]
    public void Invoke_DefaultPath()
    {
        var (_, exception) = Execute(
            """
            Join-Path TestDbs A | Set-Location
            $Target = New-SqlContext -DatabaseName PSqlDeployTestA
            Invoke-SqlMigrations $Target -WhatIf
            """
        );

        exception.ShouldBeNull();
    }

    private static (IReadOnlyList<PSObject?>, Exception?) Execute(string script)
    {
        return ScriptExecutor.Execute(
            IntegrationTestsSetup.WithIntegrationTestDefaults,
            script
        );
    }
}
