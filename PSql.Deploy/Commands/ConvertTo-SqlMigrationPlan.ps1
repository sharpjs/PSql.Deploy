<#
    Copyright (C) 2020 Jeffrey Sharp

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

function ConvertTo-SqlMigrationPlan {
    <#
    .SYNOPSIS
        Converts a set of migrations resolved by Resolve-SqlMigrations into a plan to apply the migrations to a target database.
    #>
    param (
        # Set of migrations to convert.
        [Parameter(Mandatory)]
        [System.Collections.Specialized.OrderedDictionary]
        $Migrations,

        # Path of directory in which to save the plan scripts.
        [Parameter(Mandatory)]
        [string]
        $Path
    )

    <#
        Case 1: No dependencies
        =======================

        These migrations:
        1 | Pre Core Post
        2 | Pre Core Post
        3 | Pre Core Post
        4 | Pre Core Post
        5 | Pre Core Post

        Yield this order operations:
          | Pre                | Core                     | Post                     |
        --------------------------------------------------------------------------------
        1 | Pre                | Core                     | Post                     |
        2 |     Pre            |      Core                |      Post                |
        3 |         Pre        |           Core           |           Post           |
        4 |             Pre    |                Core      |                Post      |
        5 |                Pre |                     Core |                     Post |
            Time-->

        Case 2: One dependency
        ======================

        These migrations:
        1 | Pre Core Post
        2 | Pre Core Post<--,
        3 | Pre Core Post   |
        4 | Pre Core Post---'  Migration4 depends on Migration2
        5 | Pre Core Post

        Yield this order operations:
          | Pre         | Core                                       | Post           |
        -------------------------------------------------------------------------------
        1 | Pre         | Core           Post                        |                |
        2 |     Pre     |      Core      ^^^^ Post                   |                |
        3 |         Pre |           Core      ^^^^                   | Post           |
        4 |             |                          Pre     Core      |      Post      |
        5 |             |                          ^^^ Pre      Core |           Post |
          Time-->                                      ^^^     

        This was deemed too optimistic:
          | Pre             | Core                              | Post                 |
        --------------------------------------------------------------------------------
        1 | Pre             | Core                              | Post                 |
        2 |     Pre         |      Core Post                    |                      |
        3 |         Pre     |           ^^^^ Core               |      Post            |
        4 |                 |                     Pre Core      |            Post      |
        5 |             Pre |                     ^^^      Core |                 Post |
          Time-->

        This was deemed too pessimistic:
          | Pre | Core                                                         | Post |
        -------------------------------------------------------------------------------
        1 | Pre | Core Post                                                    |      |
        2 |     |           Pre Core Post                                      |      |
        3 |     |                         Pre Core Post                        |      |
        4 |     |                                       Pre Core Post          |      |
        5 |     |                                                     Pre Core | Post |
          Time-->                                              

        Rules
        =====

        Pre  = Pres from migrations before any that depend on an unfinished migration.
        Core = Everything else.
        Post = Posts from migrations after any that are depended upon by an unfinished migration.

        Migration N's Pres  are guaranteed to run after all of Migration N-1's Pres.
        Migration N's Cores are guaranteed to run after all of Migration N-1's Cores.
        Migration N's Posts are guaranteed to run after all of Migration N-1's Posts.

        Only the greatest dependency name for each migration matters.
    #>

    # Phases
    $Pre  = [PSCustomObject] @{ Number = 1; Name = "Pre";  Count = 0; IsUsed = $false }
    $Core = [PSCustomObject] @{ Number = 2; Name = "Core"; Count = 0; IsUsed = $false }
    $Post = [PSCustomObject] @{ Number = 3; Name = "Post"; Count = 0; IsUsed = $false }

    # Compute ideal ordering, irrespective of dependencies
    $Tasks = @(foreach ($Phase in $Pre, $Core, $Post) {
        $Migrations.Values `
            | ? { $_.State -lt $Phase.Number } `
            | % {
                $Phase.Count++
                [PSCustomObject] @{
                    Done      = $false
                    Phase     = $Phase
                    Migration = $_
                    Depends   = $_ `
                        | % Depends `
                        | ? { $Migrations[$_].State -lt $Post.Number } `
                        | ConvertTo-StringHashSet
                }
            }
    })

    # Initialize target directory
    $Path = New-Item $Path -Type Directory -Force | % FullName
    $Path | Join-Path -ChildPath * | Remove-Item -Recurse

    # Write Pre phase
    # Stop after last Pre phase or before any Pre phase that depends on something
    Optimize-SqlMigrationPlanPhase $Pre $Tasks `
        -StopIf { param($Task) $Task.Phase.Name -ne "Pre" -or $Task.Depends.Count } `
        | Out-File -LiteralPath (Join-Path $Path 1_Pre.sql) -Encoding utf8 -Force

    # Output Core phase
    # Stop after last migration Core phase is visited
    Optimize-SqlMigrationPlanPhase $Core $Tasks `
        -StopIf { param($Task) !$Core.Count } `
        -SkipIf { param($Task) $Task.Depends.Count } `
        | Out-File -LiteralPath (Join-Path $Path 2_Core.sql) -Encoding utf8 -Force

    # Output Post phase
    # Stop when all phases are visited
    Optimize-SqlMigrationPlanPhase $Post $Tasks `
        | Out-File -LiteralPath (Join-Path $Path 3_Post.sql) -Encoding utf8 -Force

    [PSCustomObject] @{
        Path            = $Path
        RequiresOffline = $Core.IsUsed
    }
}

function Optimize-SqlMigrationPlanPhase {
    param (
        [Parameter(Mandatory)]
        [object] $Phase,

        [Parameter(Mandatory)]
        [AllowEmptyCollection()]
        [object[]] $Tasks,

        [Parameter()]
        [scriptblock] $StopIf,

        [Parameter()]
        [scriptblock] $SkipIf
    )

    Write-Verbose "Computing $($Phase.Name) Phase"

    $Start = 0
    $Count = $Tasks.Length

    # Make multiple passes through tasks.
    # Stop when until-condition is met or all tasks are done.
    while ($Start -lt $Count) {
        for ($i = $Start; $i -lt $Count; $i++) {
            $Task = $Tasks[$i]
            Write-Verbose "Considering $($Task.Migration.Name)[$($Task.Phase.Name)]"

            # Skip task if done
            if ($Task.Done) {
                Write-Verbose "...already visited"
                if ($i -eq $Start) { $Start++ }
                continue
            }

            # Check if should stop
            if ($StopIf -and $StopIf.Invoke($Task)) {
                Write-Verbose "...should stop assembling this phase"
                return
            }

            # Check if should skip further tasks of same phase
            if ($SkipIf -and $SkipIf.Invoke($Task)) {
                Write-Verbose "...should skip further tasks from the $($Task.Phase.Name) Phase"
                for ($j = $i + 1; $j -le $Count; $i = $j++) {
                    if ($Tasks[$j].Phase -ne $Tasks[$i].Phase) { break }
                }
                continue
            }

            # Output migration Pre phase
            Write-Verbose "...writing to $($Phase.Name) Phase"
            Format-SqlMigrationPhase $Task.Migration $Task.Phase

            # Record task as done
            $Task.Done = $true
            $Task.Phase.Count--

            # Handle when entire migration is done
            if ($Task.Phase.Number -eq 3 <# Post #>) {
                # Remove dependencies on that migration
                if ($Tasks | ? { $_.Depends.Remove($Task.Migration.Name) }) {
                    # Dependency was actually removed; restart scan from $Start
                    # to catch earlier tasks that can be visited now.
                    break
                }
            }

            # If we revisit, skip earlier tasks
            if ($i -eq $Start) { $Start++ }
         }
    }
}

function ConvertTo-StringHashSet {
    [OutputType([System.Collections.Generic.HashSet[string]])]
    param (
        [Parameter(ValueFromPipeline)]
        [string[]] $Items
    )

    begin {
        $Comparer = [StringComparer]::OrdinalIgnoreCase
        $HashSet  = New-Object System.Collections.Generic.HashSet[string] $Comparer
    }

    process {
        $Items | % { [void] $HashSet.Add($_) }
    }

    end {
        Write-Output $HashSet -NoEnumerate
    }
}

function Format-SqlMigrationPhase {
    param (
        [Parameter(Mandatory)]
        [object] $Migration,

        [Parameter(Mandatory)]
        [object] $Phase
    )

    $Name = $Migration.Name -replace "'", "''"
    $Sql  = $Migration | % "$($Phase.Name)Sql"

    # If migration has actual code to run in this phase, mark the phase as used.
    if ($Sql) { $Phase.IsUsed = $true }

    Write-Output @"
GO

-- -----------------------------------------------------------------------------
PRINT '*** $Name $($Phase.Name) ***';
GO
"@
    Write-Output $Sql

    # Ensure psedo-migrations (_Begin, _End) never get registered
    if ($Migration.IsPseudo) { return }

    Write-Output @"
GO

-- -----------------------------------------------------------------------------
PRINT '+ updt _deploy.Migration ($Name $($Phase.Name) done)';
GO

MERGE _deploy.Migration dst
USING
    (
        SELECT
            Name = '$Name',
            Hash = '$($Migration.Hash)',
            Date = SYSUTCDATETIME()
    ) src
ON src.Name = dst.Name
WHEN MATCHED
    THEN UPDATE SET
        dst.Hash = src.Hash,
        dst.$($Phase.Name)RunDate = src.Date
WHEN NOT MATCHED BY TARGET
    THEN INSERT
        (Name, Hash, $($Phase.Name)RunDate)
    VALUES
        (Name, Hash, Date)
;

IF @@ROWCOUNT != 1
    THROW 50000, 'Migration registration for $Name failed.', 0;
"@
}
