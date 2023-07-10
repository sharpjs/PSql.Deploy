// Copyright 2023 Subatomix Research Inc.
// SPDX-License-Identifier: ISC

namespace PSql.Deploy.Migrations;

/// <summary>
///   Outcome of migration application for a target database.
/// </summary>
internal enum MigrationTargetDisposition
{
    /// <summary>
    ///   All outstanding migrations were applied to the target database.
    /// </summary>
    Successful,

    /// <summary>
    ///   Migration application stopped early for the target database due to
    ///   some cause not related to the target database, leaving one or more
    ///   migration(s) unapplied.
    /// </summary>
    Incomplete,

    /// <summary>
    ///   Migration application for the target database threw an exception.
    /// </summary>
    Failed,
}
