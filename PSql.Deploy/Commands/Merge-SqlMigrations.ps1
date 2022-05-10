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

function Merge-SqlMigrations {
    <#
    .SYNOPSIS
        Merges a list of locally-defined migrations with a list of migrations already applied to a target database.

        Returns an ordered dictionary of migrations, keyed by migration name.
    #>
    [OutputType([System.Collections.Specialized.OrderedDictionary])]
    param (
        # Locally-defined migrations.
        [Parameter(Mandatory, Position=0)]
        [AllowEmptyCollection()]
        [object[]] $SourceMigrations,

        # Migrations already applied to the target database.
        [Parameter(Mandatory, Position=1)]
        [AllowEmptyCollection()]
        [object[]] $TargetMigrations
    )

    # NOTE: Cannot sort a [PSCustomObject[]] in-place with [Array]::Sort due
    # to odd PS behavior: PS appears to pass a copy of the array to the method,
    # so any in-place modifications are lost.
    $Comparer          = [StringComparer]::OrdinalIgnoreCase
    $SourceMigrations_ = [System.Collections.Generic.List[object]]::new($SourceMigrations)
    $TargetMigrations_ = [System.Collections.Generic.List[object]]::new($TargetMigrations)
    $SourceMigrations_.Sort({ param($x, $y) $Comparer.Compare($x.Name, $y.Name) })
    $TargetMigrations_.Sort({ param($x, $y) $Comparer.Compare($x.Name, $y.Name) })

    $Migrations  = [ordered] @{}
    $SourceItems = $SourceMigrations_.GetEnumerator()
    $TargetItems = $TargetMigrations_.GetEnumerator()
    $HasSource   = $SourceItems.MoveNext()
    $HasTarget   = $TargetItems.MoveNext()

    while ($HasSource -or $HasTarget) {
        # Decide which migration comes next: source, target, or both
        $Comparison =
            if     (!$HasSource) { +1 } <# Use TargetMigration #> `
            elseif (!$HasTarget) { -1 } <# Use SourceMigration #> `
            else                 { $Comparer.Compare($SourceItems.Current.Name,
                                                     $TargetItems.Current.Name) }

        # Consume that/those migration(s), potentionally merging
        switch ($Comparison) {
            { $_ -lt 0 } {
                # Use SourceMigration
                $Migration = $SourceItems.Current | Copy-SqlMigrationObject
                $HasSource = $SourceItems.MoveNext()
                Write-Host ("    (s--0) {0}" -f $Migration.Name)
                break
            }
            { $_ -gt 0 } {
                # Use TargetMigration
                $Migration = $TargetItems.Current | Copy-SqlMigrationObject
                $HasTarget = $TargetItems.MoveNext()
                if ($Migration.State -lt 3) {
                    Write-Host ("    (-t-{1}) {0}" -f $Migration.Name, $Migration.State)
                }
                break
            }
            default {
                # Merge TargetMigration into SourceMigration
                $Migration            = $SourceItems.Current | Copy-SqlMigrationObject
                $Migration.State      = $TargetItems.Current.State
                $Migration.HasChanged = $TargetItems.Current.Hash -and $TargetItems.Current.Hash -ne $Migration.Hash
                $HasSource            = $SourceItems.MoveNext()
                $HasTarget            = $TargetItems.MoveNext()
                if ($Migration.State -lt 3 -or $Migration.HasChanged) {
                    Write-Host (
                        "    (st{2}{1}) {0}" -f
                        $Migration.Name,
                        $Migration.State,
                        ($Migration.HasChanged ? '!' : '=')
                    )
                }
                break
            }
        }

        # Add to result
        $Migrations.Add($Migration.Name, $Migration)
    }

    # Return result
    $Migrations
}
