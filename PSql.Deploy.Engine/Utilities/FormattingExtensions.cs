// Copyright Subatomix Research Inc.
// SPDX-License-Identifier: MIT

using PSql.Deploy.Migrations;

namespace PSql.Deploy;

using static MigrationState;

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
}
