<#
    Part of: PSqlDeploy - Simple PowerShell Cmdlets for SQL Server Database Deployment
    https://github.com/sharpjs/PSqlDeploy

    Copyright (C) 2017 Jeffrey Sharp

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

function New-SqlMigrationObject {
    <#
    .SYNOPSIS
        Creates an empty descriptor object for a SQL migration.
    #>
    param ()

    [PSCustomObject] @{
        _Type      = "SqlMigration"
        Name       = [string]   $null  # Name of the migration
        Path       = [string]   $null  # Full path of the migration's main SQL file
        Hash       = [string]   $null  # Hash computed from the migration's SQL files
        State      = [int]      0      # Deployment state: 0 => not deployed, 1-3 => phases pre/core/post deployed
        Depends    = [string[]] $null  # Names of migrations that must be done before this one
        PreSql     = [string]   $null  # SQL script for Pre phase
        CoreSql    = [string]   $null  # SQL script for Core phase
        PostSql    = [string]   $null  # SQL script for Post phase
        IsPseudo   = [bool]     $false # If this is a _Begin or _End pseudo-migration
        HasChanged = [bool]     $false # If this migration has changed since it was applied
    }
}
