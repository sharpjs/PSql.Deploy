// Copyright Subatomix Research Inc.
// SPDX-License-Identifier: MIT

namespace PSql.Deploy.Migrations;

internal class WhatIfMigrationTargetConnection : WhatIfTargetConnection, IMigrationTargetConnection
{
    private readonly WhatIfMigrationState _state;

    public WhatIfMigrationTargetConnection(
        IMigrationTargetConnection connection,
        WhatIfMigrationState       state)
        : base(connection)
    {
        if (state is null)
            throw new ArgumentNullException(nameof(state));

        _state = state;
    }

    protected new IMigrationTargetConnection UnderlyingConnection
        => (IMigrationTargetConnection) base.UnderlyingConnection;

    /// <inheritdoc/>
    public async Task<IReadOnlyList<Migration>> GetAppliedMigrationsAsync(
        string?           minimumName,
        CancellationToken cancellation)
    {
        var migrations = await UnderlyingConnection
            .GetAppliedMigrationsAsync(minimumName, cancellation);

        foreach (var migration in migrations)
            migration.State = _state.GetState(Target, migration);

        return migrations;
    }

    /// <inheritdoc/>
    public Task InitializeMigrationSupportAsync(CancellationToken cancellation)
    {
        Log("Would initialize migration support.");

        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task ExecuteMigrationContentAsync(
        Migration         migration,
        MigrationPhase    phase,
        CancellationToken cancellation)
    {
        Log($"Would execute migration '{migration.Name}' {phase} content.");

        _state.OnApplied(Target, migration, phase);

        return Task.CompletedTask;
    }
}
