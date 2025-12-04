# PSql.Deploy

PSql.Deploy is a simple, yet versatile database migration and seeding system
for SQL Server and Azure SQL Database.

- Write migrations and seeds in plain T-SQL.
- Run migrations and seeds with PowerShell commands.
- Supports zero-downtime deployment across multiple databases.
- SQLCMD-compatible – `GO`, `$(var)`, `:setvar`, and `:r` (include) work as expected.
- Diagnostics – see the entire batch that caused an error.

## Status

[![Build](https://github.com/sharpjs/PSql.Deploy/workflows/Build/badge.svg)](https://github.com/sharpjs/PSql.Deploy/actions)
[![NuGet](https://img.shields.io/powershellgallery/v/PSql.Deploy.svg)](https://www.powershellgallery.com/packages/PSql.Deploy)
[![NuGet](https://img.shields.io/powershellgallery/dt/PSql.Deploy.svg)](https://www.powershellgallery.com/packages/PSql.Deploy)

Version 3 recently released.  Based on previous work used privately in
production for years.

## Installation

Install [this PowerShell module](https://www.powershellgallery.com/packages/PSql.Deploy).

## Usage

For now, invoke the following command for more documentation:

```powershell
Get-Help about_PSql_Deploy
```

Better README content is forthcoming.

<!--
  Copyright Subatomix Research Inc.
  SPDX-License-Identifier: MIT
-->
