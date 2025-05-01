// Copyright Subatomix Research Inc.
// SPDX-License-Identifier: MIT

namespace PSql.Deploy.Migrations;

/// <summary>
///   Contextual information provided to <see cref="MigrationValidator"/>.
/// </summary>
internal interface IMigrationValidationContext
{
    /// <inheritdoc cref="IMigrationSession.CurrentPhase"/>
    MigrationPhase Phase { get; }

    /// <inheritdoc cref="Target.ServerDisplayName"/>
    string ServerName { get; }

    /// <inheritdoc cref="Target.DatabaseDisplayName"/>
    string DatabaseName { get; }

    /// <inheritdoc cref="IMigrationSession.EarliestDefinedMigrationName"/>
    string EarliestDefinedMigrationName { get; }

    /// <inheritdoc cref="IMigrationSession.AllowContentInCorePhase"/>
    public bool AllowCorePhase { get; }
}
