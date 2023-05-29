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

function New-SqlMigrationPlan {
    <#
    .SYNOPSIS
        Computes a plan to apply migrations to target databases.
    #>
    [CmdletBinding()]
    param (
        # Path of directory containing database source code.
        [Parameter(Mandatory, Position = 0)]
        [string] $SourcePath,

        # Target database specification(s).  Create using New-SqlContext.
        [Parameter(Mandatory, Position = 1)]
        [PSql.SqlContext[]] $Target,

        # Path of directory in which to save the migration plan.  The default value is ".migration-plan".
        [string] $PlanPath = $DefaultPlanPath
    )

    process {
        # Discover migrations in source directory
        Write-Verbose "Discovering migrations in source directory."
        $SourceMigrations = @(Find-SqlMigrations $SourcePath)
        if ($SourceMigrations.Length -gt 0) {
            Write-Verbose "Discovered $($SourceMigrations.Length) source migration(s)."
        } else {
            throw "No migrations found in source directory: $SourcePath"
        }

        # Initialize plan directory
        Write-Verbose "Initializing plan directory."
        $PlanPath = New-Item $PlanPath -Type Directory -Force | ForEach-Object FullName
        $PlanPath | Join-Path -ChildPath * | Remove-Item -Recurse

        # Create subplan for each target database
        $Subplans = $(foreach ($T in $Target) {
            Write-Host "Computing plan for database '$($T.DatabaseName)' on server '$($T.ServerName)'."

            # Discover migrations applied to database
            Write-Verbose "Discovering migrations applied to database '$($T.DatabaseName)' on server '$($T.ServerName)'."
            $TargetMigrations = @(Get-SqlMigrationsApplied $T)
            Write-Verbose "Discovered $($TargetMigrations.Length) applied migration(s)."

            # Merge into a unified migrations table
            Write-Verbose "Merging migrations list."
            $Migrations = Merge-SqlMigrations $SourceMigrations $TargetMigrations

            # Add the _Begin and _End pseudo-migrations
            Find-SqlMigrations $SourcePath -Type Begin | ForEach-Object { $Migrations.Insert(0, $_.Name, $_) }
            Find-SqlMigrations $SourcePath -Type End   | ForEach-Object { $Migrations.Add(      $_.Name, $_) }

            # Validate migrations and read SQL as needed
            Resolve-SqlMigrations $Migrations

            # Make the plan
            $DatabaseId = "$($T.ServerName);$($T.DatabaseName)" -replace '[\\/:*?"<>|]', '_'
            $TargetPath = Join-Path $PlanPath $DatabaseId
            ConvertTo-SqlMigrationPlan $Migrations $TargetPath
        })

        [PSCustomObject] @{
            Path            = $PlanPath
            RequiresOffline = !!($Subplans | Where-Object RequiresOffline)
        }
    }
}
