// Copyright 2023 Subatomix Research Inc.
// SPDX-License-Identifier: ISC

namespace PSql.Deploy.Migrations;

/// <summary>
///   Contextual information provided to <see cref="MigrationValidator"/>.
/// </summary>
internal interface IMigrationValidationContext
{
    /// <inheritdoc cref="IMigrationSession.Phase"/>
    MigrationPhase Phase { get; }

    /// <inheritdoc cref="SqlContextWork.ServerDisplayName"/>
    string ServerName { get; }

    /// <inheritdoc cref="SqlContextWork.DatabaseDisplayName"/>
    string DatabaseName { get; }

    /// <inheritdoc cref="IMigrationSession.EarliestDefinedMigrationName"/>
    string EarliestDefinedMigrationName { get; }

    /// <inheritdoc cref="IMigrationSession.AllowCorePhase"/>
    public bool AllowCorePhase { get; }
}
