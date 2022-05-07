<#
.SYNOPSIS
    Invokes various build commands.

.DESCRIPTION
    This script is similar to a makefile.

    Copyright 2022 Jeffrey Sharp

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
[CmdletBinding(DefaultParameterSetName="Test")]
param (
    # Clean.
    [Parameter(Mandatory, ParameterSetName="Clean")]
    [switch] $Clean
,
    # Build.
    [Parameter(Mandatory, ParameterSetName="Build")]
    [switch] $Build
,
    # Build, run tests.
    [Parameter(ParameterSetName="Test")]
    [switch] $Test
,
    # Build, run tests, produce code coverage report.
    [Parameter(Mandatory, ParameterSetName="Coverage")]
    [switch] $Coverage
,
    # Do not build before running tests.
    [Parameter(ParameterSetName="Test")]
    [Parameter(ParameterSetName="Coverage")]
    [switch] $NoBuild
,
    # The configuration to build: Debug or Release.  The default is Debug.
    [Parameter(ParameterSetName="Build")]
    [Parameter(ParameterSetName="Test")]
    [Parameter(ParameterSetName="Coverage")]
    [ValidateSet("Debug", "Release")]
    [string] $Configuration = "Debug"
,
    # Update .NET CLI 'local tool' plugins.
    [Parameter(Mandatory, ParameterSetName="UpdateLocalTools")]
    [switch] $UpdateLocalTools
)

#Requires -Version 5
$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

$Command = $PSCmdlet.ParameterSetName
if ($Command -eq "Test") { $Test = $true }

# http://patorjk.com/software/taag/#p=display&f=Slant
Write-Host -ForegroundColor Cyan @' 

        ____  _____       __  ____             __           
       / __ \/ ___/____ _/ / / __ \___  ____  / /___  __  __
      / /_/ /\__ \/ __ `/ / / / / / _ \/ __ \/ / __ \/ / / /
     / ____/___/ / /_/ / / / /_/ /  __/ /_/ / / /_/ / /_/ / 
    /_/    /____/\__, /_(_)_____/\___/ .___/_/\____/\__, /  
                   /_/              /_/            /____/   
'@

function Main {
    if ($UpdateLocalTools) {
        Update-LocalTools
        return
    }

    if ($Clean) {
        Invoke-Clean
        return
    }

    if (!$NoBuild) {
        Invoke-Build
    }

    if ($Test -or $Coverage) {
        Invoke-Test
    }

    if ($Coverage) {
        Export-CoverageReport
    }
} 

function Update-LocalTools {
    Write-Phase "Update Local Tools"
    Invoke-DotNet tool update dotnet-reportgenerator-globaltool
}

function Invoke-Clean {
    Write-Phase "Clean"
    Invoke-Git -Arguments @(
        "clean", "-fxd",      # Delete all untracked files in directory tree
        "-e", "*.suo",        # Keep Visual Studio <  2015 local options
        "-e", "*.user",       # Keep Visual Studio <  2015 local options
        "-e", ".vs/"          # Keep Visual Studio >= 2015 local options
    )
}

function Invoke-Build {
    Write-Phase "Build"
    Invoke-DotNet build --configuration:$Configuration
}

function Invoke-Test {
    Write-Phase "Test$(if ($Coverage) {" + Coverage"})"
    Remove-Item coverage\raw -Recurse -ErrorAction SilentlyContinue
    Invoke-DotNet -Arguments @(
        "test"
        "--nologo"
        "--no-build"
        "--configuration:$Configuration"
        if ($Coverage) {
            "--settings:Coverlet.runsettings"
            "--results-directory:coverage\raw"
        }
    )
}

function Export-CoverageReport {
    Write-Phase "Coverage Report"
    Invoke-DotNet -Arguments "tool", "restore"
    Invoke-DotNet -Arguments @(
        "reportgenerator"
        "-reports:coverage\raw\**\coverage.opencover.xml"
        "-targetdir:coverage"
        "-reporttypes:Cobertura;JsonSummary;Html_Dark;Badges"
        "-verbosity:Warning"
    )
    $Summary = (Get-Content coverage\Summary.json -Raw | ConvertFrom-Json).summary
    @(
        ""
        "Coverage:"
        "    Methods:  {0,7:F3}%" -f $Summary.methodcoverage
        "    Lines:    {0,7:F3}%" -f $Summary.linecoverage
        "    Branches: {0,7:F3}%" -f $Summary.branchcoverage
        ""
    ) | Write-Host
    if ($Summary.methodcoverage + $Summary.linecoverage + $Summary.branchcoverage -lt 300) {
        Write-Warning "Coverage is below 100%."
    }
}

function Invoke-DotNet {
    param (
        [Parameter(Mandatory, ValueFromRemainingArguments)]
        [string[]] $Arguments
    )
    & dotnet $Arguments
    if ($LASTEXITCODE -ne 0) { throw "dotnet exited with an error." }
}

function Invoke-Git {
    param (
        [Parameter(Mandatory, ValueFromRemainingArguments)]
        [string[]] $Arguments
    )
    & git $Arguments
    if ($LASTEXITCODE -ne 0) { throw "git exited with an error." }
}

function Write-Phase {
    param (
        [Parameter(Mandatory)]
        [string] $Name
    )
    Write-Host "`n===== $Name =====`n" -ForegroundColor Cyan
}

# Invoke Main
try {
    Push-Location $PSScriptRoot
    Main
}
finally {
    Pop-Location
}
