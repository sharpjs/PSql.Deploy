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

function New-SqlContextParallelSet {
    <#
    .SYNOPSIS
        Creates an object describing how to connect to a set of databases and the maximum degree of parallelism to use when performing an operation against the set.
    #>
    [CmdletBinding()]
    [OutputType([PSql.SqlContextParallelSet])]
    param (
        # Informational name of the set.
        [Parameter(Position = 0)]
        [string] $Name,

        # Objects specifying how to connect to the databases in the set.  Obtain via the New-SqlContext cmdlet.
        [Parameter(Position = 1, ValueFromPipeline)]
        [PSql.SqlContext[]] $Context,

        # Maximum count of operations to perform in parallel.  The default value is the number of virtual processors on the local machine.
        [Parameter()]
        [Alias("Parallelism")]
        [ValidateRange(1, [int]::MaxValue)]
        [int] $ThrottleLimit = [Environment]::ProcessorCount
    )

    begin {
        $Set = [PSql.SqlContextParallelSet]::new()
    }

    process {
        if ($Name) {
            $Set.Name = $Name
        }
        if ($Context) {
            $Context | ForEach-Object { $Set.Contexts.Add($_) }
        }
        if ($ThrottleLimit) {
            $Set.Parallelism = $ThrottleLimit
        }
    }

    end {
        $Set
    }
}
