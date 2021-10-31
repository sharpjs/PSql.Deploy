# _Post

param (
    # Target database specification.  Create using New-SqlContext.
    [Parameter(Mandatory)]
    [PSql.SqlContext] $Target,

    # Full path of the directory of the seed being executed.
    [Parameter(Mandatory)]
    [string] $SeedPath,

    # Name/value pairs to define as SqlCmd variables.
    [Parameter(Mandatory)]
    [hashtable] $Define
)

Write-Host "This is in _Post.ps1"
