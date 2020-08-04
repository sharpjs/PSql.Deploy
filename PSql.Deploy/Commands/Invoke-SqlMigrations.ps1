<#
    Part of: PSqlDeploy - Simple PowerShell Cmdlets for SQL Server Database Deployment
    https://github.com/sharpjs/PSqlDeploy

    Copyright (C) 2018 Jeffrey Sharp

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

function Invoke-SqlMigrations {
    <#
    .SYNOPSIS
        A convenience wrapper for migration cmdlets.  Updates migration support on target databases, creates a migration plan, and runs the migration plan against the target databases.
    #>
    [CmdletBinding(SupportsShouldProcess, ConfirmImpact='High')]
    param (
        # Path of directory containing database source code.
        [Parameter(Mandatory, Position = 0)]
        [string] $SourcePath,

        # Target database specification(s).  Create using New-SqlContext.
        [Parameter(Mandatory, Position = 1)]
        [object[]] $Target,

        # Path of directory in which to save the migration plan.  The default value is "SqlMigrationPlan".
        [string] $PlanPath = $DefaultPlanPath,

        # Do not prompt for confirmation if migration(s) require applications to be offline.
        [switch] $Force
    )

    $ErrorActionPreference = "Stop"
    $OldConfirmPreference  = $ConfirmPreference
    $ConfirmPreference     = "High"

    Install-SqlMigrationSupport $Target

    $Plan = New-SqlMigrationPlan $SourcePath $Target -PlanPath $PlanPath

    $ConfirmPreference = $OldConfirmPreference

    if ($Plan.RequiresOffline -and -not $Force -and -not $PSCmdlet.ShouldProcess(
        "",
        "Have you ensured that the above conditions are met?",
        "***** WARNING *****`n" +
        "The migration(s) to be applied contain breaking changes that require applications to be offline.  " +
        "Continue ONLY when:`n" +
        "  * downtime is expected; and,`n" +
        "  * applications using the target database(s) have been taken offline.`n`n"
    )) { return }

    $ConfirmPreference = "High"

    Invoke-SqlMigrationPlan Pre $Target -PlanPath $PlanPath

    if ($Plan.RequiresOffline) {
        Invoke-SqlMigrationPlan Core $Target -PlanPath $PlanPath
    }

    Invoke-SqlMigrationPlan Post $Target -PlanPath $PlanPath
}
