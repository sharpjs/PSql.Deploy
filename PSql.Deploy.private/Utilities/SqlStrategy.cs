// Copyright 2023 Subatomix Research Inc.
// SPDX-License-Identifier: ISC

namespace PSql.Deploy;

internal static class SqlStrategy
{
    public static ISqlStrategy GetInstance(bool isWhatIfMode)
        => isWhatIfMode
            ? NullSqlStrategy   .Instance
            : DefaultSqlStrategy.Instance;
}
