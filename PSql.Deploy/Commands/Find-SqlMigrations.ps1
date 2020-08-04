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

function Find-SqlMigrations {
    <#
    .SYNOPSIS
        Discovers migrations in the specified source directory.

    .DESCRIPTION
        This cmdlet expects a filesystem layout like the following:

        Database\               The "source directory": a set of migrations and
          |                       seeds for one database.  The name can vary.
          |
          > Migrations\         Migrations for the database.
          |   |
          |   > 2017-08-17-a\   One migration.
          |       > _Main.sql   Top-level file for the migration.  It can
          |       |               include other files with the :r directive.
          |       > FileA.sql   Example file included by _Main.sql.
          |       > FileB.sql   Example file included by _Main.sql.
          |
          > Seeds\              Seeds for the the database.
          |
          > ...\                Other directories as desired.
    
        Each subdirectory of $SourceDirectory\Migrations containing a _Main.sql or _Main.Up.sql file is presumed to be a migration.  The name of the subdirectory is the name of the migration.  The search is not recursive; only one level of subdirectories is examined.

        Migrations named _Begin or _End, if present, are special pseudo-migrations intended for setup and teardown scripts.  These scripts are run before and after any named (normal) migrations.

        For each migration discovered, this cmdlet outputs a descriptor object with Name, Path, and IsPseudo properties set.
    #>
    param (
        # Path to directory containing database source code.  The default is the current directory.
        [Parameter(ValueFromPipeline)]
        [string] $SourceDirectory = ".",

        # Type of migration to discover: Named, Begin, or End.  The default is Named.
        [ValidateSet("Named", "Begin", "End")]
        [string] $Type = "Named"
    )

    # Verify the source directory exists.
    # If not, the thrown exception will mention the caller's specified path.
    $SourceDirectory = Convert-Path $SourceDirectory

    # Choose which migrations to discover
    $Pattern = $(switch ($Type) {
        Named { "*"      }
        Begin { "_Begin" }
        End   { "_End"   }
    })

    # Choose which migrations to exclude
    $Excludes = $(switch ($Type) {
        Named { "_Begin", "_End" }
        Begin { @()              }
        End   { @()              }
    })

    # Find migrations
    "_Main.sql", "_Main.Up.sql" `
        | % { Join-Path $SourceDirectory Migrations\$Pattern\$_ } `
        | Get-Item -Exclude $Excludes -ErrorAction SilentlyContinue `
        | group Directory `
        | sort Name `
        | % { $_ | % Group | sort Name | select -First 1 } `
        | % {
            $Migration          = New-SqlMigrationObject
            $Migration.Name     = $_.Directory.Name
            $Migration.Path     = $_.FullName
            $Migration.Hash     = Get-SqlMigrationHash $_.Directory.FullName
            $Migration.IsPseudo = $Type -ne "Named"
            $Migration
        }
}
