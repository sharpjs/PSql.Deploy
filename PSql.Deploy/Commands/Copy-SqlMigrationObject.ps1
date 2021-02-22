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

function Copy-SqlMigrationObject {
    <#
    .SYNOPSIS
        Creates a descriptor object for a SQL migration that is a clone of an existing object.
    #>
    param (
        # The SQL migration descriptor object
        [Parameter(Mandatory, Position=0, ValueFromPipeline)]
        [PSCustomObject] $Source
    )

    [PSCustomObject] @{
        _Type      = "SqlMigration"
        Name       = $Source.Name       # Name of the migration
        Path       = $Source.Path       # Full path of the migration's main SQL file
        Hash       = $Source.Hash       # Hash computed from the migration's SQL files
        State      = $Source.State      # Deployment state: 0 => not deployed, 1-3 => phases pre/core/post deployed
        Depends    = $Source.Depends    # Names of migrations that must be done before this one
        PreSql     = $Source.PreSql     # SQL script for Pre phase
        CoreSql    = $Source.CoreSql    # SQL script for Core phase
        PostSql    = $Source.PostSql    # SQL script for Post phase
        IsPseudo   = $Source.IsPseudo   # If this is a _Begin or _End pseudo-migration
        HasChanged = $Source.HasChanged # If this migration has changed since it was applied
    }
}
