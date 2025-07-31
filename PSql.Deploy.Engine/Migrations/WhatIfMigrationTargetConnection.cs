// Copyright Subatomix Research Inc.
// SPDX-License-Identifier: MIT

namespace PSql.Deploy.Migrations;

/// <summary>
///   A connection to a target database that can discover applied migrations
///   but will simulate execution of schema migrations or any other requested
///   database changes.
/// </summary>
internal class WhatIfMigrationTargetConnection : WhatIfTargetConnection, IMigrationTargetConnection
{
    private readonly WhatIfMigrationState _state;

    /// <summary>
    ///   Initializes a new <see cref="WhatIfMigrationTargetConnection"/>
    ///   instance wrapping the specified connection.
    /// </summary>
    /// <param name="connection">
    ///   The connection to be wrapped.
    /// </param>
    /// <param name="state">
    ///   What-if simulation state.
    /// </param>
    /// <exception cref="ArgumentNullException">
    ///   <paramref name="connection"/> and/or
    ///   <paramref name="state"/> is <see langword="null"/>.
    /// </exception>
    public WhatIfMigrationTargetConnection(
        IMigrationTargetConnection connection,
        WhatIfMigrationState       state)
        : base(connection)
    {
        if (state is null)
            throw new ArgumentNullException(nameof(state));

        _state = state;
    }

    /// <inheritdoc cref="WhatIfTargetConnection.UnderlyingConnection"/>
    protected new IMigrationTargetConnection UnderlyingConnection
        => (IMigrationTargetConnection) base.UnderlyingConnection;

    /// <inheritdoc/>
    public async Task<IReadOnlyList<Migration>> GetAppliedMigrationsAsync(
        string?           minimumName,
        CancellationToken cancellation)
    {
        var migrations = await UnderlyingConnection
            .GetAppliedMigrationsAsync(minimumName, cancellation);

        return _state.Get(Target, migrations);
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
        ArgumentNullException.ThrowIfNull(migration);

        Log($"Would execute migration '{migration.Name}' {phase} content.");

        _state.OnApplied(Target, migration, phase);

        return Task.CompletedTask;
    }
}
