/*
    Copyright (C) 2020 Jeffrey Sharp

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
*/

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

-- Retire old migration registry, if it exists.
IF OBJECT_ID('_deploy.AppliedMigrations', 'U') IS NOT NULL
BEGIN
    INSERT _deploy.Migration
        (Name, Hash, PreRunDate, CoreRunDate, PostRunDate)
    SELECT
        Name, Hash='', DateApplied, DateApplied, DateApplied
    FROM
        _deploy.AppliedMigrations
    ;

    DROP TABLE _deploy.AppliedMigrations;
END;
