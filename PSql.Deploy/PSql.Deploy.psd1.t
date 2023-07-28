# {Copyright}
# SPDX-License-Identifier: ISC
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
    PowerShellVersion    = '7.0'
    RequiredModules      = @(
        @{
            ModuleName    = 'PSql'
            ModuleVersion = '3.0.0'
            GUID          = '218cb4b2-911a-46b1-b47c-d3504acd4627'
        }
    )
    RequiredAssemblies   = @('PSql.Deploy.Core')

    # Initialization
    #ScriptsToProcess = @(...)
    #TypesToProcess   = @(...)
    #FormatsToProcess = @(...)
    NestedModules     = @('PSql.Deploy.psm1')

    # Exports
    # NOTE: Use empty arrays to indicate no exports.
    VariablesToExport    = @()
    AliasesToExport      = @()
    DscResourcesToExport = @()
    CmdletsToExport      = @(
        'Get-SqlMigrations'
        'Invoke-SqlMigrations'
        'New-SqlContextParallelSet'
    )
    FunctionsToExport    = @(
        'Install-SqlMigrationSupport'
        'Invoke-SqlSeed'
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
                "SQL", "Server", "Azure", "Migration", "Schema",
                "PSEdition_Core", "Windows", "Linux", "MacOS"
            )
        }
    }
}
