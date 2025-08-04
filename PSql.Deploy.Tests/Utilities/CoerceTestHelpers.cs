// Copyright Subatomix Research Inc.
// SPDX-License-Identifier: MIT

namespace PSql.Deploy;

internal static class CoerceTestHelpers
{
    internal static PSObject InPSObject(this object obj)
        => new(obj);
}
