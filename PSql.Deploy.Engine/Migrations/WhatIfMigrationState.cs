// Copyright Subatomix Research Inc.
// SPDX-License-Identifier: MIT

using System.Collections.Concurrent;

namespace PSql.Deploy.Migrations;

internal class WhatIfMigrationState
{
    private readonly ConcurrentDictionary<(Target, string), MigrationState> _state;

    public WhatIfMigrationState()
    {
        _state = new();
    }

    public MigrationState GetState(Target target, Migration migration)
    {
        var key = GetKey(target, migration);

        return _state.GetOrAdd(key, migration.State);
    }

    public void OnApplied(Target target, Migration migration, MigrationPhase phase)
    {
        var key = GetKey(target, migration);

        if (migration.IsAppliedThrough(phase))
            throw new ArgumentOutOfRangeException(nameof(phase));

        _state[key] = (MigrationState) (phase + 1);
    }

    private static (Target, string) GetKey(Target target, Migration migration)

    {
        if (target is null)
            throw new ArgumentNullException(nameof(target));
        if (migration is null)
            throw new ArgumentNullException(nameof(migration));

        return (target, migration.Name);
    }
}
