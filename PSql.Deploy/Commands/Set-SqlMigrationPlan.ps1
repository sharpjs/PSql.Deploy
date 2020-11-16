<#
    Copyright 2020 Jeffrey Sharp

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

function Set-SqlMigrationPlan {
    <#
    .SYNOPSIS
        Computes a plan to apply migrations to a target database.
    #>
    [CmdletBinding(DefaultParameterSetName="IntegratedAuth")]
    param (
        # Path to directory containing database source code.
        [Parameter(Mandatory, Position = 0)]
        [string] $SourceDirectory,

        # Name of the target database.
        [Parameter(Mandatory, Position = 1)]
        [string] $Database,

        # Name of the database server.  Must be a valid hostname or IP address, with an optional instance suffix (ex: "10.12.34.56\DEV").  A dot (".") may be used to specify a local server.
        [Parameter(Position = 2)]
        [string] $Server = ".",

        # Credential to use when connecting to the database server.  If not provided, integrated authentication is used.
        [Parameter(Mandatory, ParameterSetName="CredentialAuth")]
        [pscredential] $Credential
    )

    # Discover migrations in source directory
    Write-Verbose "Discovering migrations in source directory."
    $SourceMigrations = Find-SqlMigrations $SourceDirectory

    # Discover migrations applied to database
    Write-Verbose "Discovering migrations applied to database."
    $Connection = $null
    try {
        $As               = if ($Credential) { @{ Credential = $Credential } } else { @{} }
        $Context          = PSql\New-SqlContext -ServerName $Server -DatabaseName $Database @As
        $Connection       = PSql\Connect-Sql -Context $Context
        $TargetMigrations = Get-SqlMigrationsApplied $Connection
    }
    finally {
        PSql\Disconnect-Sql $Connection
    }

    # Merge into a unified migrations table
    Write-Verbose "Merging migrations list."
    $Migrations = Merge-SqlMigrations $SourceMigrations $TargetMigrations

    # Add the _Begin and _End pseudo-migrations
    Find-SqlMigrations $SourceDirectory -Type Begin | % { $Migrations.Insert(0, $_.Name, $_) }
    Find-SqlMigrations $SourceDirectory -Type End   | % { $Migrations.Add(      $_.Name, $_) }

    # Validate migrations and read SQL as needed
    Resolve-SqlMigrations $Migrations

    # Make the plan
    ConvertTo-SqlMigrationPlan $Migrations
}
