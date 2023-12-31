// Copyright 2023 Subatomix Research Inc.
// SPDX-License-Identifier: ISC

using PSql.Deploy.Migrations;

namespace PSql.Deploy;

using static MigrationPhase;
using static MigrationState;
using static TargetDisposition;

internal static class FormattingExtensions
{
    internal static string GetFixedWidthStatusString(this Migration migration)
    {
        if (migration.Path is null)
            return "Missing";

        if (migration.HasChanged)
            return "Changed";

        if (migration.Diagnostics.Any(d => d.IsError))
            return "Invalid";

        return "Ok     ";
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
            NotApplied  => "(new)        ",
            AppliedPre  => "Pre          ",
            AppliedCore => "Pre>Core     ",
            _           => "Pre>Core>Post",
        };
    }

    internal static string? ToMarker(this TargetDisposition disposition)
    {
        return disposition switch
        {
            Successful => null,
            Incomplete => " [INCOMPLETE]",
            _          => " [EXCEPTION]",
        };
    }
}
