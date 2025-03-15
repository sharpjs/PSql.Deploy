// Copyright Subatomix Research Inc.
// SPDX-License-Identifier: MIT

namespace PSql.Deploy;

internal static class SqlStrategy
{
    public static ISqlStrategy GetInstance(bool isWhatIfMode)
        => isWhatIfMode
            ? NullSqlStrategy   .Instance
            : DefaultSqlStrategy.Instance;
}
