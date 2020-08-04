<#
    Part of: PSqlDeploy - Simple PowerShell Cmdlets for SQL Server Database Deployment
    Copyright (C) 2017 Jeffrey Sharp
    https://github.com/sharpjs/PSqlDeploy

    Permission is hereby granted, free of charge, to any person obtaining a copy
    of this software and associated documentation files (the "Software"), to deal
    in the Software without restriction, including without limitation the rights
    to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
    copies of the Software, and to permit persons to whom the Software is
    furnished to do so, subject to the following conditions:

    The above copyright notice and this permission notice shall be included in all
    copies or substantial portions of the Software.

    THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
    IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
    FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
    AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
    LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
    OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
    SOFTWARE.
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
        $As = if ($Credential) { @{ Credential = $Credential } } else { @{} }
        $Connection = PSql\Connect-Sql $Server $Database @As
        $TargetMigrations = Get-SqlMigrationsApplied $Connection
    }
    finally {
        Disconnect-Sql $Connection
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
