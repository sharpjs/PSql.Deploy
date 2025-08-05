# {Copyright}
# SPDX-License-Identifier: MIT
@{
    # Identity
    GUID          = '0d5df8dd-afcc-42cc-9175-a8dac81f779e'
    RootModule    = 'PSql.Deploy.dll'
    ModuleVersion = '{VersionPrefix}'

    # General
    Description = 'A simple, yet versatile database migration and seeding system for SQL Server and Azure SQL Database.'
    Author      = 'Jeffrey Sharp'
    CompanyName = 'Subatomix Research Inc.'
    Copyright   = '{Copyright}'

    # Requirements
    CompatiblePSEditions = 'Core'
    PowerShellVersion    = '7.2'
    RequiredModules      = @() # Designed to work with PSql, but can work independently
    RequiredAssemblies   = @()

    # Initialization
    #ScriptsToProcess = @(...)
    TypesToProcess    = @('PSql.Deploy.types.ps1xml')
    #FormatsToProcess = @(...)
    #NestedModules    = @(...)

    # Exports
    # NOTE: Use empty arrays to indicate no exports.
    VariablesToExport    = @()
    AliasesToExport      = @()
    DscResourcesToExport = @()
    FunctionsToExport    = @()
    CmdletsToExport      = @(
        'Get-SqlMigrations'
        #'Invoke-ForEachSqlTargetDatabase' # FUTURE: Provide this cmdlet
        'Invoke-SqlMigrations'
        'Invoke-SqlSeed'
        'New-SqlTargetDatabaseGroup'
        if ($env:PSQL_DEPLOY_TESTING -eq "1") {
            'Test-AsyncPSCmdlet'
            'Test-CmdletExtensions'
        }
    )

    # Discoverability and URLs
    PrivateData = @{
        PSData = @{
            # Additional metadata
            Prerelease   = '{VersionSuffix}'
            ProjectUri   = 'https://github.com/sharpjs/PSql.Deploy'
            ReleaseNotes = "https://github.com/sharpjs/PSql.Deploy/blob/master/CHANGES.md"
            LicenseUri   = 'https://github.com/sharpjs/PSql.Deploy/blob/master/LICENSE.txt'
            IconUri      = 'https://github.com/sharpjs/PSql.Deploy/blob/master/icon.png'
            Tags         = @(
                'SQL', 'Server', 'Azure', 'Migration', 'Schema',
                'PSEdition_Core', 'Windows', 'Linux', 'MacOS'
            )
        }
    }
}
