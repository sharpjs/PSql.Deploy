// Copyright 2023 Subatomix Research Inc.
// SPDX-License-Identifier: ISC

namespace PSql.Deploy.Migrations;

using static MigrationPhase;
using static MigrationState;
using static MigrationTargetDisposition;

internal static class FormattingExtensions
{
    internal static string GetFixedWithFileStatusString(this Migration migration)
    {
        return migration.HasChanged switch
        {
            true                              => "Changed",
            false when migration.Path is null => "Missing",
            _                                 => "Ok     ",
        };
    }

    internal static string ToFixedWidthString(this MigrationPhase phase)
    {
        return phase switch
        {
            Pre  => "Pre ",
            Core => "Core",
            _    => "Post",
        };
    }

    internal static string ToFixedWidthString(this MigrationState state)
    {
        return state switch
        {
            NotApplied  => "(new)          ",
            AppliedPre  => "Pre            ",
            AppliedCore => "Pre->Core      ",
            _           => "Pre->Core->Post",
        };
    }

    internal static string? ToMarker(this MigrationTargetDisposition disposition)
    {
        return disposition switch
        {
            Successful => null,
            Incomplete => " [INCOMPLETE]",
            _          => " [EXCEPTION]",
        };
    }
}
