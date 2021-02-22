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

function Resolve-SqlMigrations {
    <#
    .SYNOPSIS
        Validates a set of migrations produced by Merge-SqlMigrations.
    #>
    param (
        # Set of migrations to validate.
        [Parameter(Mandatory)]
        [System.Collections.Specialized.OrderedDictionary]
        $Migrations
    )

    $HasWarnings = $false

    foreach ($Migration in $Migrations.Values) {

        # If the migration has been applied already (partially or fully), then
        # it is possible that the migration's code has changed in the interim.
        # That suggests an accidental change to an old migration -- an error.
        # To detect this, we store a hash value in the target database's
        # migration record and compare it (in Merge-SqlMigrations) to the
        # current files' hash.  If the hashes are different, the HasChanges
        # property is set to $true.
        if ($Migration.State -gt 0 -and $Migration.HasChanged) {
            Write-Warning (
                "Migration '", $Migration.Name, "' has been applied ",
                "through phase ", $Migration.State, " of 3, ",
                "but its code in the source directory does not match the code previously used.  ",
                "To resolve, verify that no accidental changes have occurred to this migration's code.  ",
                "Then update the migration's expected hash in the _deploy.Migration table.  ",
                "Expected hash: ", $Migration.Hash `
                -join ""
            )
            $HasWarnings = $true
        }

        # How much of migration has been applied to the target database?
        if ($Migration.State -ge 3 <# Done #>) {
            # Fully applied; no further work to do in this function.
            continue
        }

        # Migration is partially applied, or perhaps never applied.
        # Check if the path to its code is known.
        if (-not $Migration.Path) {
            Write-Warning (
                "Migration ", $Migration.Name, " is partially applied ",
                "(through phase ", $Migration.State, " of 3), ",
                "but no code for it was found in the source directory.  ",
                "It is not possible to complete this migration." `
                -join ""
            )
            $HasWarnings = $true
            continue
        }

        # Path to migration code is known.

        # Read and collate migration SQL phases
        $Sql = Read-SqlMigration $Migration.Path
        $Migration.PreSql  = $Sql.PreSql
        $Migration.CoreSql = $Sql.CoreSql
        $Migration.PostSql = $Sql.PostSql

        # Resolve migration dependencies
        $Migration.Depends = @($Sql.Depends | % {
            # Look up the dependency
            $Depend = $Migrations[$_]

            # Verify dependency was found
            if (-not $Depend) {
                Write-Warning (
                    "Migration ", $Migration.Name, " claims to depend on $_, ",
                    "but no migration by that name was found.  ",
                    "The dependency cannot be satisfied." `
                    -join ""
                )
                $HasWarnings = $true
                continue
            }

            # Verify dependency runs earlier than depending migration
            if ($Depend.Name -ge $Migration.Name -and $Depend.State -ne 3 <# Done #>) {
                Write-Warning (
                    "Migration ", $Migration.Name, " claims to depend on ", $Depend.Name, ", ",
                    "but that migration must run later in the sequence.  ",
                    "The dependency cannot be satisfied." `
                    -join ""
                )
                $HasWarnings = $true
                continue
            }

            $Depend.Name
        })
    }

    if ($HasWarnings) {
        $PSCmdlet.ThrowTerminatingError((New-Error `
            "Migration(s) failed validation.  Address warnings and try again." `
            -Id PSqlDeploy.InvalidMigrations
        ))
    }
}
