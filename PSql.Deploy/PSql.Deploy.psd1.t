<#
    {Copyright}

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
@{
    # Identity
    GUID          = '0d5df8dd-afcc-42cc-9175-a8dac81f779e'
    RootModule    = 'PSql.Deploy.psm1'
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
            ModuleVersion = '2.0.0'
            GUID          = '218cb4b2-911a-46b1-b47c-d3504acd4627'
        }
    )
    RequiredAssemblies   = @(
        'Subatomix.PowerShell.TaskHost.dll'
    )

    # Initialization
    #ScriptsToProcess = @(...)
    #TypesToProcess   = @(...)
    #FormatsToProcess = @(...)
    #NestedModules    = @(...)

    # Exports
    # NOTE: Use empty arrays to indicate no exports.
    VariablesToExport    = @()
    AliasesToExport      = @()
    DscResourcesToExport = @()
    CmdletsToExport      = @()
    FunctionsToExport    = @(
        'Find-SqlMigrations'
        'Get-SqlMigrationsApplied'
        'Get-SqlMigrationHash'
        'Install-SqlMigrationSupport'
        'Invoke-SqlMigrations'
        'Invoke-SqlMigrationPlan'
        'Invoke-SqlSeed'
        'New-SqlMigrationPlan'
        'Read-SqlMigration'
        'Set-SqlMigrationPlan'
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
