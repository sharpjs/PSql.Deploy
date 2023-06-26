// Copyright 2023 Subatomix Research Inc.
// SPDX-License-Identifier: ISC

using System.Diagnostics;

namespace PSql.Deploy.Migrations;

/// <summary>
///   Runs SQL migrations.
/// </summary>
public class MigrationEngine
{
    /// <summary>
    ///   Initializes a new <see cref="MigrationEngine"/> instance for the
    ///   specified cmdlet.
    /// </summary>
    /// <param name="logger">
    ///   The logger to use to output status and messages.
    /// </param>
    /// <param name="cancellation">
    ///   The token to monitor for cancellation requests.
    /// </param>
    /// <exception cref="ArgumentNullException">
    ///   <paramref name="logger"/> is <see langword="null"/>.
    /// </exception>
    public MigrationEngine(IMigrationLogger logger, CancellationToken cancellation)
    {
        if (logger is null)
            throw new ArgumentNullException(nameof(logger));

        Migrations        = Array.Empty<Migration>();
        Logger            = logger;
        CancellationToken = cancellation;
        _totalStopwatch   = new();
    }

    /// <summary>
    ///   Gets the migrations to be applied to targets.
    /// </summary>
    public IReadOnlyList<Migration> Migrations { get; private set; }

    /// <summary>
    ///   Gets or sets the phase of the migrations to be applied to targets.
    /// </summary>
    public MigrationPhase Phase { get; set; }

    /// <summary>
    ///   Gets the logger to use to output status and messages.
    /// </summary>
    public IMigrationLogger Logger { get; }

    /// <summary>
    ///   Gets the token to monitor for cancellation requests.
    /// </summary>
    public CancellationToken CancellationToken { get; }

    private readonly Stopwatch _totalStopwatch;

    /// <summary>
    ///   Discovers migrations in the specified path.
    /// </summary>
    /// <param name="path">
    ///   The path in which to discover migrations.
    /// </param>
    public void AddMigrationsFromPath(string path)
    {
        Migrations = LocalMigrationDiscovery.GetLocalMigrations(path);
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

        await Task.WhenAll(
            targets.Select(RunAsync)
        );
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
    public async Task RunAsync(SqlContextParallelSet targets)
    {
        if (targets is null)
            throw new ArgumentNullException(nameof(targets));

        _totalStopwatch.Start();

        await Task.WhenAll(
            // TODO: Limit parallelism
            targets.Contexts.Select(RunAsync)
        );
    }

    /// <summary>
    ///   Applies migrations to the specified target asynchronously.
    /// </summary>
    /// <param name="target">
    ///   The target to which to apply migrations.
    /// </param>
    /// <returns>
    ///   A <see cref="Task"/> representing the asynchronous operation.
    /// </returns>
    public async Task RunAsync(SqlContext target)
    {
        if (target is null)
            throw new ArgumentNullException(nameof(target));

        _totalStopwatch.Start();

        // Get migrations on target
        // TODO: Limit to unfinished or not-older-than-what's-on-disk migrations
        var migrations = await RemoteMigrationDiscovery
            .GetServerMigrationsAsync(target, Logger, CancellationToken);

        // Merge source and target migration lists
        migrations = Merge(Migrations, migrations);

        // Validate
        Validate(migrations, target);

        // Run
        await RunCoreAsync(migrations, target);
    }

    private async Task RunCoreAsync(IReadOnlyList<Migration> migrations, SqlContext target)
    {
        var connection = null as ISqlConnection;
        var command    = null as ISqlCommand;
        var count      = 0;
        var exception  = null as Exception;
        var stopwatch  = new Stopwatch();

        try
        {
            stopwatch.Start();

            foreach (var migration in migrations)
            {
                // TODO: Use MigrationPlanner to decide which migrations to run

                ReportApplying(migration, target);

                connection ??= target.Connect(null, Logger);
                command    ??= connection.CreateCommand();

                await RunCoreAsync(migration, command);

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

            if (command is not null)
                await command.DisposeAsync();

            if (connection is not null)
                await connection.DisposeAsync();
        }
    }

    private Task RunCoreAsync(Migration migration, ISqlCommand command)
    {
        var sql = migration.GetSql(Phase);
        if (sql.IsNullOrEmpty())
            return Task.CompletedTask;
 
        command.CommandText    = sql;
        command.CommandTimeout = 0; // No timeout

        return command.UnderlyingCommand.ExecuteNonQueryAsync(CancellationToken);
    }

    private IReadOnlyList<Migration> Merge(
        IReadOnlyList<Migration> sourceMigrations,
        IReadOnlyList<Migration> targetMigrations)
    {
        // Assume migrations already sorted using MigrationComparer

        var migrations = new List<Migration>();

        using var sourceItems = sourceMigrations.GetEnumerator();
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

        return migrations;
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

    private bool Validate(IReadOnlyList<Migration> migrations, SqlContext target)
    {
        var valid = true;

        foreach (var migration in migrations)
        {
            valid &= ValidateNotChanged(migration, target);

            if (migration.IsAppliedThrough(Phase))
                continue; // Migration will not be applied

            valid &= ValidateCanApplyThroughPhase(migration, target);
            valid &= ValidateHasSource           (migration, target);
        }

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

        Logger.LogWarning(string.Format(
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

        Logger.LogWarning(string.Format(
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

        Logger.LogWarning(string.Format(
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

    private void ReportApplying(Migration migration, SqlContext target)
    {
        Logger.Log(new ApplyingMigrationMessage(
            migration, target, Phase, _totalStopwatch.Elapsed
        ));
    }

    private void ReportApplied(int count, SqlContext target, TimeSpan elapsed, Exception? exception)
    {
        Logger.Log(new AppliedMigrationsMessage(
            count, target, Phase, elapsed, _totalStopwatch.Elapsed, exception
        ));
    }
}
