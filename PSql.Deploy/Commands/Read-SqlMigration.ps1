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

$ChunksRe = [regex] '(?mnsx)
    # m: ^/$ match BOL/EOL
    # n: named captures only
    # s: . includes \n
    # x: ignore pattern whitespace

    # Start where previous match ended
    \G

    # Match a chunk of SQL text
    (?<text>
        (   [^''\[/-]                               # regular character
        |   '' ( [^''] | '''' )* ( ''    | \z )     # string
        |   \[ ( [^\]] | \]\] )* ( \]    | \z )     # quoted identifier
        |   -- (?!\#) .*?        ( \r?\n | \z )     # line comment (non-magic)
        |   -  (?! -)                               # just a dash
        |   /  \*     .*?        ( \* /  | \z )     # block comment
        |   /  (?!\*)                               # just a slash
        )*
    )

    # Match a magic comment or end of file
    (?<magic>
        (   ^ --\# (?<cmd> .*? ) ( \r?\n | \z )     # magic comment
        |   \z                                      # end of file
        )
    )
'

$CommandRe = [regex] '(?nsx)
    \A             [\x20\t]*                        # beginning of string
    (?<name> \w+ ) [\x20\t]*                        # NAME
    (
        ( :                     [\x20\t]* )         # NAME:
        ( (?<args> [^\x20\t]+ ) [\x20\t]* )*        # NAME: ARG ARG ARG
    )?
    \z                                              # end of string
'

function Read-SqlMigration {
    [CmdletBinding()]
    param (
        # Path of the migration's main SQL file.
        [string] $Path
    )

    $Comparer = [StringComparer]::OrdinalIgnoreCase
    $Depends  = New-Object System.Collections.Generic.SortedSet[string] $Comparer
    $PreSql   = New-Object System.Text.StringBuilder 4096
    $CoreSql  = New-Object System.Text.StringBuilder 4096
    $PostSql  = New-Object System.Text.StringBuilder 4096

    # Decide the initial phase
    $Current  = switch ($Path | Split-Path | Split-Path -Leaf) {
        _Begin  { $PreSql  }
        _End    { $PostSql }
        default { $CoreSql }
    }

    # Parse into chunks
    $Chunks = Get-Content -LiteralPath $Path -Raw -Encoding UTF8 `
        | PSql\Expand-SqlCmdDirectives -Define @{ Path = Split-Path $Path } -Verbose  `
        | % { $ChunksRe.Matches($_) }

    foreach ($Chunk in $Chunks) {
        $Text  = $Chunk.Groups['text' ].Value
        $Magic = $Chunk.Groups['magic'].Value

        # Text chunk belongs to block in progress
        [void] $Current.Append($Text)

        # Detect EOF
        if (-not $Magic) { break }

        # Split magic comment, if any
        $Match = $CommandRe.Match($Chunk.Groups['cmd'].Value)
        $Name  =   $Match.Groups['name'].Value
        $Args  = @($Match.Groups['args'].Captures | % Value)

        # Interpret magic comment, if any
        switch ($Name) {
            PRE      { $Current = $PreSql  }
            CORE     { $Current = $CoreSql }
            OFFLINE  { $Current = $CoreSql }
            POST     { $Current = $PostSql }
            REQUIRES { [void] $Depends.Add($Args) }
            default  { [void] $Current.Append($Magic) } # Not our magic
        }
    }

    # Return a migration object
    [pscustomobject] @{
        Depends = $Depends
        PreSql  = $PreSql.ToString()
        CoreSql = $CoreSql.ToString()
        PostSql = $PostSql.ToString()
    }
}
