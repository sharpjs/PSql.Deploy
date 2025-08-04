-- Copyright Subatomix Research Inc.
-- SPDX-License-Identifier: MIT

IF OBJECT_ID(N'_deploy.Migration', N'U') IS NOT NULL
EXEC sp_executesql
N'
    SELECT Name, Hash, State
    FROM _deploy.Migration
    WHERE State < 3 OR Name >= ISNULL(@MinimumName, N'''')
    ORDER BY Name
;',
N'@MinimumName nvarchar(max)', @MinimumName;
