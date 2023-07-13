-- Copyright 2023 Subatomix Research Inc.
-- SPDX-License-Identifier: ISC

IF SCHEMA_ID('_deploy') IS NULL
    EXEC('CREATE SCHEMA _deploy AUTHORIZATION dbo;');
GO

IF OBJECT_ID('_deploy.Property', 'U') IS NULL
BEGIN
    -- Version 1

    CREATE TABLE _deploy.Property
    (
        Id              bit                 NOT NULL,
        Version         int                 NOT NULL,   -- Version of deployment engine
        LockOwnerId     uniqueidentifier        NULL,   -- If locked, random identifier of lock owner
        LockDate        datetime2(0)            NULL,   -- If locked, date owner last claimed ownership

        CONSTRAINT Property_PK
            PRIMARY KEY (Id),

        CONSTRAINT Property_CK_Id
            CHECK (Id = 1),

        CONSTRAINT Property_CK_Version
            CHECK (Version > 0),

        CONSTRAINT Property_CK_Lock
            CHECK (IIF(LockOwnerId IS NULL, 0, 1) = IIF(LockDate IS NULL, 0, 1))
    );

    INSERT _deploy.Property (Id, Version)
    SELECT
        Id      = 1,
        Version = 1;

    CREATE TABLE _deploy.Migration
    (
        Name            sysname         NOT NULL,   -- Name of the migration
        Hash            char(40)        NOT NULL,   -- SHA-1 hash of the migration files

        State           AS CASE
                            WHEN PostRunDate IS NOT NULL THEN 3
                            WHEN CoreRunDate IS NOT NULL THEN 2
                            WHEN PreRunDate  IS NOT NULL THEN 1
                            ELSE 0
                        END,

        PreRunDate      datetime2(0)        NULL,   -- Date when pre-deploy phase ran
        CoreRunDate     datetime2(0)        NULL,   -- Date when main deploy phase ran
        PostRunDate     datetime2(0)        NULL,   -- Date when post-deploy phase ran

        CONSTRAINT Migration_PK
            PRIMARY KEY (Name),

        CONSTRAINT Migration_CK_CoreRunDate
            CHECK (CoreRunDate IS NULL OR CoreRunDate >= PreRunDate),

        CONSTRAINT Migration_CK_PostRunDate
            CHECK (PostRunDate IS NULL OR PostRunDate >= CoreRunDate),
    );
END;
