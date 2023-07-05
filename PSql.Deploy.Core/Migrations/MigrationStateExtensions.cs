// Copyright 2023 Subatomix Research Inc.
// SPDX-License-Identifier: ISC

namespace PSql.Deploy.Migrations;

using static MigrationState;

internal static class MigrationStateExtensions
{
    internal static string ToFixedWidthProgressString(this MigrationState state)
    {
        return state switch
        {
            NotApplied  => "(new)          ",
            AppliedPre  => "Pre            ",
            AppliedCore => "Pre->Core      ",
            _           => "Pre->Core->Post",
        };
    }
}
