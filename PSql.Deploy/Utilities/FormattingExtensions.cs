// Copyright Subatomix Research Inc.
// SPDX-License-Identifier: MIT

namespace PSql.Deploy;

using static E.TargetDisposition;

internal static class FormattingExtensions
{
    public static string? ToMarker(this E.TargetDisposition disposition)
    {
        return disposition switch
        {
            Successful => null,
            Incomplete => " [INCOMPLETE]",
            _          => " [EXCEPTION]",
        };
    }
}
