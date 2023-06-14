# Copyright 2023 Subatomix Research Inc.
# SPDX-License-Identifier: ISC

[Diagnostics.CodeAnalysis.SuppressMessageAttribute(
    "PSUseDeclaredVarsMoreThanAssignments", "",
    Justification = "File defines shared values used by module functions."
)]
param()

#Requires -Version 7.0
$ErrorActionPreference = "Stop"
Set-StrictMode -Version 3.0

# Enable @Try shorthand to ignore errors
$Try = @{ ErrorAction = "SilentlyContinue" }

# Default path of directory in which to save migration plans
$DefaultPlanPath = ".migration-plan"

# Read all .ps1 files
Join-Path $PSScriptRoot Commands *.ps1 -Resolve | ForEach-Object { . $_ }
