// Copyright 2023 Subatomix Research Inc.
// SPDX-License-Identifier: ISC

using System.Diagnostics.CodeAnalysis;

namespace PSql.Deploy.Migrations;

using static MigrationPhase;

/// <summary>
///   Single-use factory that produces a <see cref="MigrationPlan"/> from a
///   given set of migrations.
/// </summary>
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

        Plan Pre  = Pres from migrations before any that depend on an uncompleted migration.
        Plan Core = Everything else.
        Plan Post = Posts from migrations after any that are depended upon by an uncompleted migration.

        Migration N's Pres  are guaranteed to run after all of Migration N-1's Pres.
        Migration N's Cores are guaranteed to run after all of Migration N-1's Cores.
        Migration N's Posts are guaranteed to run after all of Migration N-1's Posts.

        Only the greatest dependency name for each migration matters.
        If Migration A depends on Migration B, then B's Post will run before A's Pre.
    */

    private readonly ReadOnlySpan<Migration>              _migrations;
    private readonly HashSet<(Migration, MigrationPhase)> _scheduled;
    private readonly MigrationPlan                        _plan;

    /// <summary>
    ///   Initializes a new <see cref="MigrationPlanner"/> instance with the
    ///   specified migrations.
    /// </summary>
    /// <param name="migrations">
    ///   The migrations to assemble into a <see cref="MigrationPlan"/>.
    /// </param>
    public MigrationPlanner(ReadOnlySpan<Migration> migrations)
    {
        _migrations       = migrations;
        _scheduled        = new();
        _plan             = new();
    }

    /// <summary>
    ///   Assembles the migration plan.
    /// </summary>
    public MigrationPlan CreatePlan()
    {
        SchedulePre();
        ScheduleCore();
        SchedulePost();
        return _plan;
    }

    // Schedules the Pre components of migrations until one is found that
    // depends on an uncompleted migration.
    private void SchedulePre()
    {
        foreach (var migration in _migrations)
        {
            if (HasUnsatisfiedDependency(migration, out _))
                break;

            ScheduleInPre(migration);
        }
    }

    // Schedules the Core components of migrations, plus the Pre and Post
    // components that must be performed in the Core phase to provide
    // dependency guarantees.
    private void ScheduleCore()
    {
        foreach (var migration in _migrations)
        {
            if (HasUnsatisfiedDependency(migration, out var name))
                SatisfyDependencyInCore(name);

            ScheduleInCore(migration, Core);
        }
    }

    // Schedules the remaining Post components -- those not already scheduled
    // for the Core phase.
    private void SchedulePost()
    {
        foreach (var migration in _migrations)
            if (!IsScheduled(migration, Post))
                ScheduleInPost(migration);
    }

    // Schedules Pre and Post components to be performed in the Core phase to
    // provide dependency guarantees.
    private void SatisfyDependencyInCore(string name)
    {
        var satisfied = false;

        foreach (var migration in _migrations)
        {
            if (!satisfied)
            {
                // Schedule Post components early (in the Core phase) to
                // satisfy the dependency

                if (!IsScheduled(migration, Post))
                    ScheduleInCore(migration, Post);

                if (migration.Name == name)
                    satisfied = true;
            }
            else // (satisfied)
            {
                // Schedule Pre components late (in the Core phase) that could
                // not be scheduled earlier due to the unsatisfied dependency

                if (HasUnsatisfiedDependency(migration, out _))
                    break;

                if (!IsScheduled(migration, Pre))
                    ScheduleInCore(migration, Pre);
            }    
        }
    }

    private bool IsScheduled(Migration migration, MigrationPhase phase)
    {
        return _scheduled.Contains((migration, phase));
    }

    private void ScheduleInPre(Migration migration)
    {
        if (!migration.IsAppliedThrough(Pre))
            _plan.Pre.Add(migration);

        _scheduled.Add((migration, Pre));
    }

    private void ScheduleInCore(Migration migration, MigrationPhase phase)
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

    private bool HasUnsatisfiedDependency(
        Migration migration,
        [MaybeNullWhen(false)] out string name)
    {
        if (migration.ResolvedDepends is { } depends)
        {
            for (var i = depends.Count - 1; i >= 0; i--)
            {
                var depend = depends[i];

                if (depend.IsAppliedThrough(Post))
                    continue; // already applied

                if (IsScheduled(depend, Post))
                    continue; // will be applied prior to time being considered

                name = depend.Name;
                return true;
            }
        }

        name = null;
        return false;
    }
}
