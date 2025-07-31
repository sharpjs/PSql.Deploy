// Copyright Subatomix Research Inc.
// SPDX-License-Identifier: MIT

using System.Collections.Concurrent;

namespace PSql.Deploy.Migrations;

internal class WhatIfMigrationState
{
    private readonly ConcurrentDictionary<
        Target,
        SortedDictionary<string, Migration>
    > _state;

    public WhatIfMigrationState()
    {
        _state = new();
    }

    public IReadOnlyList<Migration> Get(Target target, IReadOnlyList<Migration> realMigrations)
    {
        ArgumentNullException.ThrowIfNull(target);
        ArgumentNullException.ThrowIfNull(realMigrations);

        var fakeMigrations = ForTarget(target).Values;

        using var real = realMigrations.GetEnumerator();
        using var fake = fakeMigrations.GetEnumerator();

        var hasReal = real.MoveNext();
        var hasFake = fake.MoveNext();

        var migrations = new List<Migration>(
            Math.Max(realMigrations.Count, fakeMigrations.Count)
        );

        while (hasReal || hasFake)
        {
            var comparison
                = !hasReal ? +1 // use fake migration
                : !hasFake ? -1 // use real migration
                : MigrationComparer.Instance.Compare(real.Current.Name, fake.Current.Name);

            if (comparison < 0)
            {
                // Use real migration not yet tracked in what-if state
                migrations.Add(real.Current);
                hasReal = real.MoveNext();
            }
            else if (comparison > 0)
            {
                // Use fake migration not actually known to target
                migrations.Add(fake.Current);
                hasFake = fake.MoveNext();
            }
            else
            {
                // Use real migration with tracked what-if state
                real.Current.State = fake.Current.State;
                migrations.Add(real.Current);
                hasReal = real.MoveNext();
                hasFake = fake.MoveNext();
            }
        }

        return migrations.AsReadOnly();
    }

    public void OnApplied(Target target, Migration migration, MigrationPhase phase)
    {
        ArgumentNullException.ThrowIfNull(target);
        ArgumentNullException.ThrowIfNull(migration);

        migration = GetOrAdd(target, migration);

        if (migration.IsAppliedThrough(phase))
            throw new ArgumentOutOfRangeException(nameof(phase));

        migration.State = (MigrationState) (phase + 1);
    }

    private Migration GetOrAdd(Target target, Migration migration)
    {
        var dictionary = ForTarget(target);

        return dictionary.TryGetValue(migration.Name, out var existing)
            ? existing
            : dictionary[migration.Name] = new(migration.Name)
            {
                Hash  = migration.Hash,
                State = migration.State,
            };
    }

    private SortedDictionary<string, Migration> ForTarget(Target target)
    {
        return _state.GetOrAdd(target, _ => new(MigrationComparer.Instance));
    }
}
