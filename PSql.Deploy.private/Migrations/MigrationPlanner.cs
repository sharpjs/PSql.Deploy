// Copyright 2023 Subatomix Research Inc.
// SPDX-License-Identifier: ISC

using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;

namespace PSql.Deploy.Migrations;

using static MigrationPhase;

using Phase = MigrationPhase;

internal readonly ref struct MigrationPlanner
{
    /*
        Migration Ordering

        Case 1: No dependencies
        =======================

        These migrations:
        1 │ Pre Core Post
        2 │ Pre Core Post
        3 │ Pre Core Post
        4 │ Pre Core Post
        5 │ Pre Core Post

        Yield this order operations:
          │ Pre                │ Core                     │ Post                     │
        ══╪════════════════════╪══════════════════════════╪══════════════════════════╡
        1 │ Pre                │ Core                     │ Post                     │
        2 │     Pre            │      Core                │      Post                │
        3 │         Pre        │           Core           │           Post           │
        4 │             Pre    │                Core      │                Post      │
        5 │                Pre │                     Core │                     Post │
            Time──>

        Case 2: One dependency
        ======================

        These migrations:
        1 │ Pre Core Post
        2 │ Pre Core Post<──╮
        3 │ Pre Core Post   │
        4 │ Pre Core Post───╯  Migration4 depends on Migration2
        5 │ Pre Core Post

        Yield this order operations:
          │ Pre         │ Core                                       │ Post           │
        ══╪═════════════╪════════════════════════════════════════════╪════════════════╡
        1 │ Pre         │ Core           Post                        │                │
        2 │     Pre     │      Core      ^^^^ Post                   │                │
        3 │         Pre │           Core      ^^^^                   │ Post           │
        4 │             │                          Pre     Core      │      Post      │
        5 │             │                          ^^^ Pre      Core │           Post │
          Time──>                                      ^^^

        Rules
        =====

        Σ_Pre  = Pres from migrations before any that depend on an unfinished migration.
        Σ_Core = Everything else.
        Σ_Post = Posts from migrations after any that are depended upon by an unfinished migration.

        Migration N's Pres  are guaranteed to run after all of Migration N-1's Pres.
        Migration N's Cores are guaranteed to run after all of Migration N-1's Cores.
        Migration N's Posts are guaranteed to run after all of Migration N-1's Posts.

        Only the greatest dependency name for each migration matters.
        If Migration A depends on Migration B, then B's Post will run before A's Pre.
    */

    private readonly Span<Migration>               _migrations;
    private readonly Dictionary<string, Migration> _migrationsByName;
    private readonly HashSet<(Migration, Phase)>   _scheduled;
    private readonly MigrationPlan                 _plan;

    public MigrationPlanner(Span<Migration> migrations)
    {
        _migrations       = migrations;
        _migrationsByName = new(capacity: migrations.Length, StringComparer.OrdinalIgnoreCase);
        _scheduled        = new();
        _plan             = new();
    }

    [Obsolete("Do not use.", error: true)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public MigrationPlanner() { throw new NotSupportedException(); }

    public MigrationPlan CreatePlan()
    {
        Prepare();
        SchedulePre();
        ScheduleCore();
        SchedulePost();
        return _plan;
    }

    private void Prepare()
    {
        foreach (var migration in _migrations)
        {
            // Pseudo migrations cannot be dependencies
            if (migration.IsPseudo)
                continue;

            _migrationsByName.Add(migration.Name!, migration);
        }
    }

    private void SchedulePre()
    {
        // Schedule Pre components of migrations until one is found that
        // depends on an uncompleted migration

        foreach (var migration in _migrations)
        {
            if (HasUncompletedDependency(migration))
                break;

            ScheduleInPre(migration);
        }
    }

    private void ScheduleCore()
    {
        // Stop when there are no more Cores to sequence

        foreach (var migration in _migrations)
        {
            // do dependency posts
            if (HasUncompletedDependency(migration, out var name))
                SchedulePostComponentsInCoreThrough(name);

            if (!IsScheduled(migration, Pre))
                ScheduleInCore(migration, Pre);

            ScheduleInCore(migration, Core);
        }
    }

    private void SchedulePostComponentsInCoreThrough(string name)
    {
        foreach (var migration in _migrations)
        {
            if (!IsScheduled(migration, Post))
                ScheduleInCore(migration, Post);

            if (migration.Name == name)
                break;
        }
    }

    private void SchedulePost()
    {
        // Schedule remaining migration Post components
        foreach (var migration in _migrations)
            if (!IsScheduled(migration, Post))
                ScheduleInPost(migration);
    }

    private bool IsScheduled(Migration migration, Phase phase)
    {
        return _scheduled.Contains((migration, phase));
    }

    private void ScheduleInPre(Migration migration)
    {
        if (!migration.IsAppliedThrough(Pre))
            _plan.Pre.Add(migration);

        _scheduled.Add((migration, Pre));
    }

    private void ScheduleInCore(Migration migration, Phase phase)
    {
        if (!migration.IsAppliedThrough(phase))
            _plan.Core.Add((migration, phase));

        _scheduled.Add((migration, phase));
    }

    private void ScheduleInPost(Migration migration)
    {
        if (!migration.IsAppliedThrough(Post))
            _plan.Post.Add(migration);

        _scheduled.Add((migration, Post));
    }

    private bool HasUncompletedDependency(Migration migration)
    {
        foreach (var name in migration.Depends!)
        {
            if (!_migrationsByName.TryGetValue(name, out var depend))
                throw new Exception("Can't find the dependency.");

            if (depend.IsAppliedThrough(Post))
                continue;

            return true;
        }

        return false;
    }

    private bool HasUncompletedDependency(Migration migration, [MaybeNullWhen(false)] out string latest)
    {
        latest = null;

        // TODO: This could use a reverse enumeration capability.
        foreach (var name in migration.Depends!)
        {
            if (!_migrationsByName.TryGetValue(name, out var depend))
                throw new Exception("Can't find the dependency.");

            if (depend.IsAppliedThrough(Post))
                continue;

            latest = name;
        }

        return latest is not null;
    }
}
