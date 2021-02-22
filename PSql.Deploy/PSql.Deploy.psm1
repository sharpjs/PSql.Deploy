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

#Requires -Version 7.0
$ErrorActionPreference = "Stop"
Set-StrictMode -Version 3.0

# Enable @Try shorthand to ignore errors
$Try = @{ ErrorAction = "SilentlyContinue" }

# Default path of directory in which to save migration plans
$DefaultPlanPath = "SqlMigrationPlan"

# Load module code
$PSScriptRoot `
    | Join-Path -ChildPath Commands `
    | Join-Path -ChildPath *.ps1 -Resolve `
    | ForEach-Object { . $_ }
