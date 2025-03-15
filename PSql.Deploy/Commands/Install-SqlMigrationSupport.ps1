#Requires -Version 7

<#
    Copyright Subatomix Research Inc.
    SPDX-License-Identifier: MIT
#>

function Install-SqlMigrationSupport {
    [CmdletBinding()]
    param (
        # Target database specification(s).  Create using New-SqlContext.
        [Parameter(Mandatory, Position = 1)]
        [PSql.SqlContext[]] $Target
    )

    process {
        foreach ($T in $Target) {
            Write-Host "Installing migration support in database '$($T.DatabaseName)' on server '$($T.ServerName)'."

            $Connection = $null
            try {
                $Connection = $T | PSql\Connect-Sql

                Join-Path $PSScriptRoot Install-SqlMigrationSupport.sql -Resolve `
                    | ForEach-Object { Get-Content -LiteralPath $_ -Raw -Encoding UTF8 } `
                    | PSql\Invoke-Sql -Connection $Connection -Timeout '00:01:00'
            }
            finally {
                Disconnect-Sql $Connection
            }
        }
    }
}
