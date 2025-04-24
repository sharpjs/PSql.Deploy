// Copyright Subatomix Research Inc.
// SPDX-License-Identifier: MIT

using PSql.Deploy.Migrations;

namespace PSql.Deploy;

using static MigrationPhase;
using static MigrationState;
using static TargetDisposition;

public static class FormattingExtensions
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

    public static string? ToMarker(this TargetDisposition disposition)
    {
        return disposition switch
        {
            Successful => null,
            Incomplete => " [INCOMPLETE]",
            _          => " [EXCEPTION]",
        };
    }

    internal static string Format(this SqlError item)
    {
        const int    MaxInformationalSeverity = 10;
        const string NonProcedureLocationName = "(batch)";

        var prefix = item.Class > MaxInformationalSeverity
            ? "<!> "
            : null;

        var procedure
            =  item.Procedure.NullIfEmpty()
            ?? NonProcedureLocationName;

        // Examples:
        // <!> (batch):42: 102:15:1: Incorrect syntax near 'foo'.
        // <!> MyProc:1337: E2812:L16: Could not find stored procedure 'foo'.

        return $"{prefix}{procedure}:{item.LineNumber}: E{item.Number}:L{item.Class}: {item.Message}";
    }
}
