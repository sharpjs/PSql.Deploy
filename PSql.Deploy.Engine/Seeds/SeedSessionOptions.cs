// Copyright Subatomix Research Inc.
// SPDX-License-Identifier: MIT

namespace PSql.Deploy.Seeds;

/// <summary>
///   Options for <see cref="SeedSession"/>.
/// </summary>
[Flags]
public enum SeedSessionOptions
{
    /// <summary>
    ///   Operate in what-if mode.  In this mode, a seeding session reports
    ///   what actions it would perform against a target database but does not
    ///   perform the actions.
    /// </summary>
    IsWhatIfMode = 1 << 31,
}
