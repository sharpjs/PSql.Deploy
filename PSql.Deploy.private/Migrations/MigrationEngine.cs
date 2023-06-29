// Copyright 2023 Subatomix Research Inc.
// SPDX-License-Identifier: ISC

using System.Collections.Immutable;
using System.Diagnostics;

namespace PSql.Deploy.Migrations;

/// <summary>
///   An engine that applies a set of migrations to a set of target databases.
/// </summary>
public class MigrationEngine
{
    /// <summary>
    ///   Initializes a new <see cref="MigrationEngine"/> instance.
    /// </summary>
    /// <param name="console">
    ///   The console on which to display status and messages.
    /// </param>
    /// <param name="cancellation">
    ///   The token to monitor for cancellation requests.
    /// </param>
    /// <exception cref="ArgumentNullException">
    ///   <paramref name="console"/> is <see langword="null"/>.
    /// </exception>
    public MigrationEngine(IConsole console, CancellationToken cancellation)
    {
        if (console is null)
            throw new ArgumentNullException(nameof(console));

        Migrations           = ImmutableArray<Migration>.Empty;
        MinimumMigrationName = "";
        Console              = console;
        CancellationToken    = cancellation;
        _totalStopwatch      = new();
    }

    /// <summary>
    ///   Gets the migrations to be applied to targets.
    /// </summary>
    public ImmutableArray<Migration> Migrations { get; private set; }

    /// <summary>
    ///   Gets the minimum name of the migrations to be applied to targets, or
    ///   string if <see cref="Migrations"/> is empty.
    /// </summary>
    public string MinimumMigrationName { get; private set; }

    /// <summary>
    ///   Gets or sets the phase of the migrations to be applied to targets.
    /// </summary>
    public MigrationPhase Phase { get; set; }

    /// <summary>
    ///   Gets the console on which to display status and messages.
    /// </summary>
    public IConsole Console { get; }

    /// <summary>
    ///   Gets the token to monitor for cancellation requests.
    /// </summary>
    public CancellationToken CancellationToken { get; }

    // Measures total elapsed time of migration run
    private readonly Stopwatch _totalStopwatch;

    /// <summary>
    ///   Discovers migrations in the specified path.
    /// </summary>
    /// <param name="path">
    ///   The path in which to discover migrations.
    /// </param>
    public void AddMigrationsFromPath(string path)
    {
        Migrations           = LocalMigrationDiscovery.GetLocalMigrations(path);
        MinimumMigrationName = GetMinimumMigrationName() ?? "";
    }

    /// <summary>
    ///   Applies migrations to the specified targets asynchronously.
    /// </summary>
    /// <param name="targets">
    ///   The targets to which to apply migrations.
    /// </param>
    /// <returns>
    ///   A <see cref="Task"/> representing the asynchronous operation.
    /// </returns>
    public async Task RunAsync(IReadOnlyList<SqlContextParallelSet> targets)
    {
        if (targets is null)
            throw new ArgumentNullException(nameof(targets));

        _totalStopwatch.Start();

        await Task.WhenAll(targets.Select(RunAsync));
    }

    private async Task RunAsync(SqlContextParallelSet targets)
    {
        if (targets is null)
            throw new ArgumentNullException(nameof(targets));

        await Task.Yield(); // Parallelize

        using var limiter = new SemaphoreSlim(targets.Parallelism, targets.Parallelism);

        async Task RunLimitedAsync(SqlContext target)
        {
            await limiter.WaitAsync(CancellationToken);
            try
            {
                await RunAsync(target);
            }
            finally
            {
                limiter.Release();
            }
        }

        await Task.WhenAll(targets.Contexts.Select(RunLimitedAsync));
    }

    private async Task RunAsync(SqlContext target)
    {
        if (target is null)
            throw new ArgumentNullException(nameof(target));

        await Task.Yield(); // Parallelize

        // Discover migrations on target
        var migrations = await RemoteMigrationDiscovery.GetServerMigrationsAsync(
            target, MinimumMigrationName, Console, CancellationToken
        );

        // Plan what to do
        var plan = CreatePlan(migrations, target);
        if (plan is null)
            return;

        // Run the plan
        await RunCoreAsync(plan, target);
    }

    private MigrationPlan? CreatePlan(IReadOnlyList<Migration> migrations, SqlContext target)
    {
        var merged = Merge(Migrations, migrations);

        if (!Validate(merged, target))
            return null;

        return new MigrationPlanner(merged).CreatePlan();
    }

    private async Task RunCoreAsync(MigrationPlan plan, SqlContext target)
    {
        var items = plan.GetItems(Phase);
        if (!items.Any())
            return;

        var stopwatch  = Stopwatch.StartNew();
        var count      = 0;
        var exception  = null as Exception;

        using var connection = target.Connect(null, Console);
        using var command    = connection.CreateCommand();

        try
        {
            foreach (var (migration, phase) in items)
            {
                ReportApplying    (migration, phase, target);
                await RunCoreAsync(migration, phase, command);
                count++;
            }
        }
        catch (Exception e)
        {
            exception = e;
            throw;
        }
        finally
        {
            stopwatch.Stop();
            ReportApplied(count, target, stopwatch.Elapsed, exception);
        }
    }

    private Task RunCoreAsync(Migration migration, MigrationPhase phase, ISqlCommand command)
    {
        var sql = migration.GetSql(phase);
        if (sql.IsNullOrEmpty())
            return Task.CompletedTask;
 
        command.CommandText    = sql;
        command.CommandTimeout = 0; // No timeout

        //return command.UnderlyingCommand.ExecuteNonQueryAsync(CancellationToken);

        return Task.CompletedTask;
    }

    private ReadOnlySpan<Migration> Merge(
        ImmutableArray<Migration> sourceMigrations,
        IReadOnlyList <Migration> targetMigrations)
    {
        // Assume migrations already sorted using MigrationComparer

        var migrations = ImmutableArray.CreateBuilder<Migration>( 
            initialCapacity: Math.Max(sourceMigrations.Length, targetMigrations.Count)
        );

              var sourceItems = sourceMigrations.GetEnumerator();
        using var targetItems = targetMigrations.GetEnumerator();

        var hasSource = sourceItems.MoveNext();
        var hasTarget = targetItems.MoveNext();

        while (hasSource || hasTarget)
        {
            Migration? migration;

            // Decide which migration comes next: source, target, or both
            var comparison
                = !hasSource ? +1 // use target migration
                : !hasTarget ? -1 // use source migration
                : MigrationComparer.Instance.Compare(sourceItems.Current, targetItems.Current);

            // Consume that/those migration(s), potentionally merging
            if (comparison < 0)
            {
                // source
                migration = OnSourceWithoutTarget(sourceItems.Current);
                hasSource = sourceItems.MoveNext();
            }
            else if (comparison > 0)
            {
                // target
                migration = OnTargetWithoutSource(targetItems.Current);
                hasTarget = targetItems.MoveNext();
            }
            else
            {
                // both
                migration = OnMatchedSourceAndTarget(sourceItems.Current, targetItems.Current);
                hasSource = sourceItems.MoveNext();
                hasTarget = targetItems.MoveNext();
            }

            if (migration is not null)
                migrations.Add(migration);
        }

        return migrations.Build().AsSpan();
    }

    private static Migration? OnSourceWithoutTarget(Migration source)
    {
        // Migration will be applied; ensure its content is loaded
        MigrationLoader.LoadContent(source);

        // Old log: "    (s--0) {0}" -f $Migration.Name
        return source;
    }

    private static Migration? OnTargetWithoutSource(Migration target)
    {
        if (target.IsAppliedThrough(MigrationPhase.Post))
            return null; // completed; source migration removed

        // Old log: "    (-t-{1}) {0}" -f $Migration.Name, $Migration.State
        return target;
    }

    private Migration? OnMatchedSourceAndTarget(Migration source, Migration target)
    {
        // If migration will be applied, ensure its content is loaded
        if (!target.IsAppliedThrough(Phase))
            MigrationLoader.LoadContent(source);

        // Copy source-only properties to target
        target.Path       = source.Path;
        target.HasChanged = target.Hash is not null && target.Hash != source.Hash;
        target.Hash       = source.Hash;
        target.Depends    = source.Depends;
        target.PreSql     = source.PreSql;
        target.CoreSql    = source.CoreSql;
        target.PostSql    = source.PostSql;

        if (target.IsAppliedThrough(MigrationPhase.Post) && !target.HasChanged)
            return null; // completed

        // Old log:
        // if ($Migration.State -lt 3 -or $Migration.HasChanged)
        // "    (st{2}{1}) {0}" -f $Migration.Name, $Migration.State, ($Migration.HasChanged ? '!' : '=')

        return target;
    }

    private string? GetMinimumMigrationName()
    {
        foreach (var migration in Migrations)
            if (!migration.IsPseudo)
                return migration.Name;

        return null;
    }

    private bool Validate(ReadOnlySpan<Migration> migrations, SqlContext target)
    {
        var valid  = true;
        var lookup = CreateLookup(migrations);

        foreach (var migration in migrations)
        {
            valid &= ValidateNotChanged(migration, target);
            valid &= ValidateDepends   (migration, lookup);

            if (migration.IsAppliedThrough(Phase))
                continue; // Migration will not be applied

            valid &= ValidateCanApplyThroughPhase(migration, target);
            valid &= ValidateHasSource           (migration, target);
        }

        return valid;
    }

    private Dictionary<string, Migration> CreateLookup(ReadOnlySpan<Migration> migrations)
    {
        var lookup = new Dictionary<string, Migration>(
            capacity: migrations.Length, StringComparer.OrdinalIgnoreCase
        );

        foreach (var migration in migrations)
            if (!migration.IsPseudo && migration.Name is { } name)
                lookup.Add(name, migration);

        return lookup;
    }

    private bool ValidateDepends(Migration migration, Dictionary<string, Migration> dictionary)
    {
        if (migration.IsPseudo)
            return true;

        if (migration.Depends is not { } dependNames)
            return true;

        var resolvedDepends = new List<Migration>(dependNames.Count);
        var valid           = true;

        static int Compare(string? lhs, string? rhs)
            => StringComparer.OrdinalIgnoreCase.Compare(lhs, rhs);

        foreach (var dependName in dependNames)
        {
            switch (Compare(dependName, migration.Name))
            {
                case < 0 when dictionary.TryGetValue(dependName, out var depend):
                    resolvedDepends.Add(depend);
                    break;

                case < 0 when Compare(dependName, MinimumMigrationName) < 0:
                    Console.WriteVerbose(string.Format(
                        "Ignoring migration '{0}' dependency on migration '{1}', " +
                        "which is older than the earliest migration on disk.",
                        /*{0}*/ migration.Name,
                        /*{1}*/ dependName
                    ));
                    break;

                case < 0:
                    valid = false;
                    Console.WriteWarning(string.Format(
                        "Migration '{0}' declares a dependency on migration '{1}', " +
                        "which was not found. "                                      +
                        "The dependency cannot be satisfied.",
                        /*{0}*/ migration.Name,
                        /*{1}*/ dependName
                    ));
                    break;

                case > 0:
                    valid = false;
                    Console.WriteWarning(string.Format(
                        "Migration '{0}' declares a dependency on migration '{1}', " +
                        "which must run later in the sequence. "                     +
                        "The dependency cannot be satisfied.",
                        /*{0}*/ migration.Name,
                        /*{1}*/ dependName
                    ));
                    break;

                default:
                    valid = false;
                    Console.WriteWarning(string.Format(
                        "Migration '{0}' declares a dependency on itself. " +
                        "The dependency cannot be satisfied.",
                        /*{0}*/ migration.Name
                    ));
                    break;
            }
        }

        migration.ResolvedDepends = resolvedDepends;
        return valid;
    }

    private bool ValidateNotChanged(Migration migration, SqlContext target)
    {
        // Valid regardless of hash difference if migration is not yet applied
        if (migration.State2 == MigrationState.NotApplied)
            return true;

        // Valid if hash has not changed
        if (!migration.HasChanged)
            return true;

        Console.WriteWarning(string.Format(
            "Migration '{0}' has been applied to database [{1}].[{2}] through " +
            "the {3} phase, but the migration's code in the source directory "  +
            "does not match the code previously used. To resolve, revert any "  +
            "accidental changes to this migration. To ignore, update the hash " +
            "in the _deploy.Migration table to match the hash of the code in "  +
            "the source directory ({4}).",
            /*{0}*/ migration.Name,
            /*{1}*/ target.GetEffectiveServerName(),
            /*{2}*/ target.DatabaseName,
            /*{3}*/ migration.AppliedThroughPhase,
            /*{4}*/ migration.Hash
        ));

        return false;
    }

    private bool ValidateCanApplyThroughPhase(Migration migration, SqlContext target)
    {
        if (migration.CanApplyThrough(Phase))
            return true;

        Console.WriteWarning(string.Format(
            "Cannot apply {3} phase of migration '{0}' to database [{1}].[{2}] " +
            "because the migration has code in an earlier phase that must be "   +
            "applied first.",
            /*{0}*/ migration.Name,
            /*{1}*/ target.GetEffectiveServerName(),
            /*{2}*/ target.DatabaseName,
            /*{3}*/ Phase
        ));

        return false;
    }

    private bool ValidateHasSource(Migration migration, SqlContext target)
    {
        // Valid if there is a path to the migration code
        if (migration.Path is not null)
            return true;

        Console.WriteWarning(string.Format(
            "Migration {0} is only partially applied to database [{1}].[{2}] " +
            "(through the {3} phase), but the code for the migration was not " +
            "found in the source directory. It is not possible to complete "   +
            "this migration.",
            /*{0}*/ migration.Name,
            /*{1}*/ target.GetEffectiveServerName(),
            /*{2}*/ target.DatabaseName,
            /*{3}*/ migration.AppliedThroughPhase
        ));

        return false;
    }

    private void ReportApplying(Migration migration, MigrationPhase phase, SqlContext target)
    {
        Console.WriteHost(string.Format(
            @"[+{0:hh\:mm\:ss}] {1}: Applying {2} {3}",
            _totalStopwatch.Elapsed,
            target.DatabaseName,
            migration.Name,
            phase
        ));
    }

    private void ReportApplied(int count, SqlContext target, TimeSpan elapsed, Exception? exception)
    {
        Console.WriteHost(string.Format(
            @"[+{0:hh\:mm\:ss}] {1}: Applied {2} {3} item(s) in {4:N3} second(s){5}",
            _totalStopwatch.Elapsed,
            target.DatabaseName,
            count,
            Phase,
            elapsed.TotalSeconds,
            exception is null ? null : " [EXCEPTION]"
        ));
    }
}
