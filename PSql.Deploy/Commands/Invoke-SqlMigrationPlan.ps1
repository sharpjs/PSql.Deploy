<#
    Copyright (C) 2020 Jeffrey Sharp

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
    param (
        # Migration phase to run.  Must be "Pre", "Core", or "Post".
        [Parameter(Mandatory, Position = 0)]
        [ValidateSet("Pre", "Core", "Post")]
        [string] $Phase,

        # Target database specification(s).  Create using New-SqlContext.
        [Parameter(Mandatory, Position = 1, ValueFromPipeline)]
        [PSql.SqlContext[]] $Target,

        # Path of directory containing the migration plan.  The default value is "SqlMigrationPlan".
        [string] $PlanPath = $DefaultPlanPath
    )

    $ScriptFile = $(switch ($Phase) {
        Pre  { "1_Pre.sql"  }
        Core { "2_Core.sql" }
        Post { "3_Post.sql" }
    })

    foreach ($T in $Target) {
        Write-Host "--------------------------------------------------------------------------------"
        Write-Host "Migration phase $Phase for database '$($T.DatabaseName)' on server '$($T.ServerName)'."
        Write-Host "--------------------------------------------------------------------------------"

        $Connection = $null
        try {
            $Connection = $T | PSql\Connect-Sql
            Convert-Path -LiteralPath $PlanPath `
                | Join-Path -ChildPath "$($T.ServerName);$($T.DatabaseName)" `
                | Join-Path -ChildPath $ScriptFile `
                | Foreach-Object { Get-Content -LiteralPath $_ -Raw -Encoding UTF8 } `
                | PSql\Invoke-Sql -Connection $Connection
        }
        finally {
            PSql\Disconnect-Sql $Connection
        }
    }
}
