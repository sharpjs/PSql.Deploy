<#
    Copyright 2021 Jeffrey Sharp

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

using namespace PSql.Deploy.Seeding

function Invoke-SqlSeed {
    <#
    .SYNOPSIS
        Applies seeds to target databases.

    .DESCRIPTION
        This cmdlet expects a filesystem layout like the following:

        Database\               The "source directory": a set of migrations and
          |                       seeds for one database.  The name can vary.
          |
          > Migrations\         Migrations for the database.
          |
          > Seeds\              Seeds for the the database.
          |   |
          |   > Foo\            One seed.  The name can vary.
          |       > _Main.sql   Top-level file for the seed.  It can include
          |       |               other files with the :r directive.
          |       | _Pre.ps1    PowerShell script that runs before _Main.sql.
          |       | _Post.ps1   PowerShell script that runs after _Main.sql.
          |       > FileA.sql   Example file included by _Main.sql.
          |       > FileB.sql   Example file included by _Main.sql.
          |
          > ...\                Other directories as desired.

        Each subdirectory of $SourceDirectory\Seeds containing a _Main.sql file is presumed to be a seed.  The name of the subdirectory is the name of the seed.  The search is not recursive; only one level of subdirectories is examined.
    #>
    [CmdletBinding()]
    param (
        # Names of the seeds to run.
        [Parameter(Mandatory, Position=0)]
        [string[]]
        $Seed,

        # Objects specifying how to connect to target databases.  Obtain via the New-SqlContext cmdlet.
        [Parameter(Mandatory, Position=1, ValueFromPipeline)]
        [PSql.SqlContext[]]
        $Target,

        # Path to a directory containing database source code.  The default is the current directory.
        [Parameter()]
        [string]
        $SourcePath = ".",

        # Name/value pairs to define as SqlCmd variables.
        [Parameter()]
        [hashtable]
        $Define = @{},

        # Number of databases to seed simultaneously.
        [Parameter()]
        [ValidateRange(1, [int]::MaxValue)]
        [int]
        $DatabaseParallelism,

        # Number of commands to perform simultaneously against a particular database.
        [Parameter()]
        [ValidateRange(1, [int]::MaxValue)]
        [int]
        $CommandParallelism
    )

    begin {
        # The core script that each parallel worker runs
        # NOTE: Use $Seed to access the seed context
        $WorkerScript = {
            $ErrorActionPreference = "Stop"
            Set-StrictMode -Version 3.0
            Import-Module PSql

            $Target = [PSql.SqlContext] $Seed.Data['Target']
            $Connection = Connect-Sql -Context $Target
            try {
                Invoke-Sql -Connection $Connection "
                    DECLARE @id uniqueidentifier = '$($Seed.RunId)';
                    SET CONTEXT_INFO @id;
                "
                while ($Module = $Seed.GetNextModule()) {
                    Invoke-Sql $Module.Batches -Connection $Connection
                }
            }
            finally {
                Disconnect-Sql $Connection
            }
        }

        # Create factory to build each seed plan
        $PlanFactory = [SeedPlanFactory]::new($WorkerScript, $Host)
    }

    process {
        # Get seed _Main.sql files
        $Mains = $Seed | ForEach-Object { Join-Path $SourcePath Seeds $_ _Main.sql } | Get-Item

        # Assert _Main.sql files are readable
        $Mains | Get-Content -Head 1 > $null

        # Translate -DatabaseParallelism argument
        $Limit = $DatabaseParallelism ? @{ ThrottleLimit = $DatabaseParallelism } : @{}

        # Run seeds against each target in parallel
        $Target | ForEach-Object @Limit -Parallel {
            $ErrorActionPreference = "Stop"
            Set-StrictMode -Version 3.0
            Import-Module PSql, PSql.Deploy

            # Run each seed in sequence
            foreach ($Main in $using:Mains) {
                $Plan = ($using:PlanFactory).Create()
                try {
                    # Flow objects to seed plan
                    $Plan.AddContextData('Target', $_)
                    $Plan.AddContextData('Name', $_.DatabaseName)

                    # Populate the seed plan with modules
                    $Main `
                        | Get-Content -Raw `
                        | Expand-SqlCmdDirectives -Define $using:Define `
                        | ForEach-Object { $Plan.AddModules($_) }

                    # Validate the seed plan
                    if (($Errors = $Plan.Validate()).Count) {
                        $Errors.GetEnumerator()
                        throw "Invalid"
                    }

                    # Run the seed plan
                    $Plan.Run($using:CommandParallelism)
                }
                finally {
                    $Plan.Dispose()
                }
            }
        }
    }
}
