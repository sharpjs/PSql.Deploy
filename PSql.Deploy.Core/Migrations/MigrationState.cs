// Copyright 2023 Subatomix Research Inc.
// SPDX-License-Identifier: ISC

namespace PSql.Deploy.Migrations;

/// <summary>
///   Application state of a migration.
/// </summary>
public enum MigrationState
{
    /// <summary>
    ///   Not applied.
    /// </summary>
    NotApplied,

    /// <summary>
    ///   Applied partially, through the <c>Pre</c> phase.
    /// </summary>
    AppliedPre,

    /// <summary>
    ///   Applied partially, through the <c>Core</c> phase.
    /// </summary>
    AppliedCore,

    /// <summary>
    ///   Applied completely, through the <c>Post</c> phase.
    /// </summary>
    AppliedPost,
}
