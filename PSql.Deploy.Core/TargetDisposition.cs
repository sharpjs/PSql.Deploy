// Copyright 2023 Subatomix Research Inc.
// SPDX-License-Identifier: ISC

namespace PSql.Deploy;

/// <summary>
///   Outcome of a set of operations executed against a target database to
///   apply schema migrations or content seeds.
/// </summary>
public enum TargetDisposition
{
    /// <summary>
    ///   All operations completed successfully.
    /// </summary>
    Successful,

    /// <summary>
    ///   Execution against the target database stopped early due to some cause
    ///   not related to the target database, leaving one or more operation(s)
    ///   incomplete.
    /// </summary>
    Incomplete,

    /// <summary>
    ///   Execution against the target database failed, leaving one or more
    ///   operation(s) incomplete.
    /// </summary>
    Failed,
}
