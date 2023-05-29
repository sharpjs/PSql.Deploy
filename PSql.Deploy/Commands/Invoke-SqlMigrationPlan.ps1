#Requires -Version 7
using namespace System.Management.Automation
using namespace System.Management.Automation.Runspaces
using namespace Subatomix.PowerShell.TaskHost

<#
    Copyright 2023 Subatomix Research Inc.

    Permission to use, copy, modify, and distribute this software for any
    purpose with or without fee is hereby granted, provided that the above
    copyright notice and this permission notice appear in all copies.

    THE SOFTWARE IS PROVIDED "AS IS" AND THE AUTHOR DISCLAIMS ALL WARRANTIES
    WITH REGARD TO THIS SOFTWARE INCLUDING ALL IMPLIED WARRANTIES OF
    MERCHANTABILITY AND FITNESS. IN NO EVENT SHALL THE AUTHOR BE LIABLE FOR
    ANY SPECIAL, DIRECT, INDIRECT, OR CONSEQUENTIAL DAMAGES OR ANY DAMAGES
    WHATSOEVER RESULTING FROM LOSS OF USE, DATA OR PROFITS, WHETHER IN AN
    ACTION OF CONTRACT, NEGLIGENCE OR OTHER TORTIOUS ACTION, ARISING OUT OF
    OR IN CONNECTION WITH THE USE OR PERFORMANCE OF THIS SOFTWARE.
#>

function Invoke-SqlMigrationPlan {
    <#
    .SYNOPSIS
        Invokes a migration plan created by New-SqlMigrationPlan.
    #>
    [CmdletBinding(DefaultParameterSetName = "Target")]
    param (
        # Migration phase to run.  Must be "Pre", "Core", or "Post".
        [Parameter(Mandatory, Position = 0)]
        [ValidateSet("Pre", "Core", "Post")]
        [string] $Phase,

        # Objects specifying how to connect to the target databases.  Obtain via the New-SqlContext cmdlet.
        [Parameter(Mandatory, Position = 1, ParameterSetName = "Target", ValueFromPipeline)]
        [PSql.SqlContext[]] $Target,

        # Objects specifying how to connect to sets of target databases.
        [Parameter(Mandatory, Position = 1, ParameterSetName = "TargetSet", ValueFromPipeline)]
        [PSql.SqlContextParallelSet[]] $TargetSet,

        # Path of directory containing the migration plan.  The default value is ".migration-plan".
        [Parameter()]
        [string] $PlanPath = $DefaultPlanPath,

        # The maximum count of target databases to migrate in parallel.  The default value is the number of virtual processors on the local machine.
        [Parameter()]
        [ValidateRange(1, [int]::MaxValue)]
        [int] $ThrottleLimit = [Environment]::ProcessorCount
    )

    begin {
        # Capture items for use in tasks
        $PSql      = Get-Module PSql
        $TSettings = [PSInvocationSettings]
        $TVariable = [SessionStateVariableEntry]

        # Create a factory for per-task hosts
        $TaskHostFactory = [Subatomix.PowerShell.TaskHost.TaskHostFactory]::new($Host)

        # Prepare a task to run for each target
        $Task = {
            Write-Host "--------------------------------------------------------------------------------"
            Write-Host "Migration phase $Phase for database '$(
                            $Target.DatabaseName)' on server '$($Target.ServerName)'."
            Write-Host "--------------------------------------------------------------------------------"

            Import-Module $PSql

            Get-Content -LiteralPath $ScriptPath -Raw `
                | PSql\Invoke-Sql -Context $Target -Timeout 0
        }.ToString()
    }

    process {
        # Select the SQL script file that contains the commands for this phase
        $ScriptFile = $(switch ($Phase) {
            Pre  { "1_Pre.sql"  }
            Core { "2_Core.sql" }
            Post { "3_Post.sql" }
        })

        if (-not $TargetSet) {
            $TargetSet = @([PSql.SqlContextParallelSet]::new($Target, $ThrottleLimit))
        }

        # Run all the target sets in parallel
        $TargetSet | ForEach-Object -ThrottleLimit 32 -Parallel {
            $Phase           = $using:Phase
            $PlanPath        = $using:PlanPath
            $ScriptFile      = $using:ScriptFile
            $PSql            = $using:PSql
            $TSettings       = $using:TSettings
            $TVariable       = $using:TVariable
            $TaskHostFactory = $using:TaskHostFactory
            $Task            = $using:Task

            # Run the SQL script file for each target
            $_.Contexts | ForEach-Object -ThrottleLimit $_.Parallelism -Parallel {
                # Create a task for this target
                $Task = [ScriptBlock]::Create($using:Task)

                # Identity which script file the task should run
                $DatabaseId = "$($_.ServerName);$($_.DatabaseName)" -replace '[\\/:*?"<>|]', '_'
                $ScriptPath = Join-Path $using:PlanPath $DatabaseId $using:ScriptFile -Resolve

                # Make a header to prefix the task's output lines
                $Header = "$($using:Phase):$($_.DatabaseName)"

                # Prepare invocation settings for the task
                $Settings                       = ($using:TSettings)::new()
                $Settings.Host                  = ($using:TaskHostFactory).Create($Header)
                $Settings.ErrorActionPreference = "Stop"

                # Prepare session state for the task
                $State = [InitialSessionState]::CreateDefault2()
                $State.Variables.Add(($using:TVariable)::new("PSql",       $using:PSql, "", "Constant,AllScope"))
                $State.Variables.Add(($using:TVariable)::new("ScriptPath", $ScriptPath, "", "Constant,AllScope"))
                $State.Variables.Add(($using:TVariable)::new("Target",     $_,          "", "Constant,AllScope"))

                # Run the task
                $Shell = [PowerShell]::Create($State)
                try {
                    $Shell.AddScript($Task).Invoke($null, $Settings)
                }
                catch {
                    $Shell.Dispose()
                }
            }
        }
    }
}
