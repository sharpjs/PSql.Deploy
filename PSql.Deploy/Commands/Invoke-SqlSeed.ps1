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

function Invoke-SqlSeed {
    <#
    .SYNOPSIS
        Runs seed script(s) on the target database(s).
    #>
    param (
        # Path of directory containing database source code.
        [Parameter(Mandatory, Position=0)]
        [string] $SourcePath,

        # Target database specification(s).  Create using New-SqlContext.
        [Parameter(Mandatory, Position=1, ValueFromPipeline)]
        [object[]] $Target,

        # Name(s) of the seed(s).
        [Parameter(Mandatory, Position=2)]
        [string[]] $Seed,

        # Name/value pairs to define as SqlCmd variables.
        [Parameter(Position=3)]
        [hashtable] $Define = @{}
    )

    $ErrorActionPreference = "Stop"

    # Resolve seeds
    $SeedMains = @(
        $Seed | % { Join-Path $SourcePath Seeds\$_\_Main.sql } | Get-Item
    )

    foreach ($T in $Target) {
        foreach ($S in $SeedMains) {
            Write-Host "--------------------------------------------------------------------------------"
            Write-Host "Seed $($S.Directory.Name) for database '$($T.Database)' on server '$($T.Server)'."
            Write-Host "--------------------------------------------------------------------------------"

            # Prepare for Expand-SqlCmdDirectives (without modifying passed hashtable)
            $MyDefine      = $Define.Clone()
            $MyDefine.Path = $S.Directory.FullName

            # Prepare for Invoke-SqlModules
            $LegacyConnectionParameters = @{
                Server   = $T.Server
                Database = $T.Database
            }
            if ($T.Credential -ne [PSCredential]::Empty) {
                $LegacyConnectionParameters.Login    = $T.Credential.UserName
                $LegacyConnectionParameters.Password = $T.Credential.GetNetworkCredential().Password
            }

            # Prepare for _Pre.ps1 and _Post.ps1
            $ScriptArgs = @{
                Target = $T
                Seed   = $S.Directory.Name
                Define = $MyDefine
            }

            # Execute _Pre.ps1
            $Script = Join-Path $S.Directory.FullName _Pre.ps1
            if (Test-Path $Script) {
                Write-Host "Running _Pre.ps1"
                & $Script @ScriptArgs
            }

            # Execute Seed SQL
            Get-Content -LiteralPath $S.FullName -Raw -Encoding UTF8 `
                | PSql\Expand-SqlCmdDirectives -Define $MyDefine `
                | PSql\Invoke-SqlModules @LegacyConnectionParameters

            # Execute _Post.ps1
            $Script = Join-Path $S.Directory.FullName _Post.ps1
            if (Test-Path $Script) {
                Write-Host "Running _Post.ps1"
                & $Script @ScriptArgs
            }
        }
    }
}
