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

function Get-SqlMigrationHash {
    <#
    .SYNOPSIS
        Computes a hash value for a SQL migration.

        The hash value is adequate for change detection, but not for cryptography.
    #>
    param (
        # Path of the directory containing the SQL migration.
        [string] $Path
    )

    # Compute a hash for each file
    $Hashes = Get-ChildItem $Path *.sql `
        | Get-FileHash -Algorithm SHA1 | % Hash `
        | Out-String

    # Prep a stream to read the hashes as binary
    $Bytes  = $Hashes | ConvertFrom-Hex
    $Stream = New-Object System.IO.MemoryStream @(,$Bytes)

    # Compute a single hash from the per-file hashes
    Get-FileHash -InputStream $Stream -Algorithm SHA1 | % Hash
}

function ConvertFrom-Hex {
    <#
    .SYNOPSIS
        Converts from a string of hex digits (1-9, a-f, A-F) to a byte array.

        Non-hex-digit characters are ignored.
    #>
    [OutputType([byte[]])]
    param (
        # String of hex digits to convert.
        [Parameter(Mandatory, Position=0, ValueFromPipeline)]
        [string] $Hex
    )

    [byte[]] ( $Hex `
        -replace '[^0-9a-fA-F]', '' `
        -split '(?<=\G..)(?=..)' `
        | % { [Convert]::ToByte($_, 16) }
    )
}
