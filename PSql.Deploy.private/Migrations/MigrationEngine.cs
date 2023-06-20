// Copyright 2023 Subatomix Research Inc.
// SPDX-License-Identifier: ISC

namespace PSql.Deploy.Migrations;

/// <summary>
///   Runs SQL migrations.
/// </summary>
public class MigrationEngine : IDisposable
{
    private readonly CancellationTokenSource _cancellation;

    /// <summary>
    ///   Initializes a new <see cref="MigrationEngine"/> instance for the
    ///   specified cmdlet.
    /// </summary>
    /// <param name="cmdlet">
    ///   The cmdlet invoking the engine.  The engine uses the output methods
    ///   of this cmdlet to report status.
    /// </param>
    /// <exception cref="ArgumentNullException">
    ///   <paramref name="cmdlet"/> is <see langword="null"/>.
    /// </exception>
    public MigrationEngine(Cmdlet cmdlet)
    {
        if (cmdlet is null)
            throw new ArgumentNullException(nameof(cmdlet));

        Migrations    = Array.Empty<Migration>();
        Cmdlet        = cmdlet;
        _cancellation = new CancellationTokenSource();
    }

    /// <summary>
    ///   Gets the cmdlet that invoked the engine.
    /// </summary>
    public Cmdlet Cmdlet { get; }

    /// <summary>
    ///   Gets the migrations to be applied to targets.
    /// </summary>
    public IReadOnlyList<Migration> Migrations { get; private set; }

    /// <summary>
    ///   Gets or sets the phase of the migrations to be applied to targets.
    /// </summary>
    public MigrationPhase Phase { get; set; }

    /// <summary>
    ///   Requests cancellation of the migration engine activity.
    /// </summary>
    public void Cancel()
        => _cancellation.Cancel();

    /// <inheritdoc/>
    public void Dispose()
        => _cancellation.Dispose();

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

        await Task.WhenAll(
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

        // Get migrations on target
        var migrations = await RemoteMigrationDiscovery
            .GetServerMigrationsAsync(target, Cmdlet, _cancellation.Token);

        // Merge source and target migration lists
        migrations = Merge(Migrations, migrations);

        // Validate
        Validate(migrations);

        // TODO...
    }

    #region Merge

    private IReadOnlyList<Migration> Merge(
        IReadOnlyList<Migration> sourceMigrations,
        IReadOnlyList<Migration> targetMigrations)
    {
        // Assume migrations already sorted, ordinal ignore-case

        var migrations = new List<Migration>();

        using var sourceItems = sourceMigrations.GetEnumerator();
        using var targetItems = targetMigrations.GetEnumerator();

        var hasSource = sourceItems.MoveNext();
        var hasTarget = targetItems.MoveNext();

        while (hasSource || hasTarget)
        {
            Migration migration;

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

            migrations.Add(migration);
        }

        return migrations;
    }

    private static Migration OnSourceWithoutTarget(Migration source)
    {
        // Migration will be applied; ensure its content is loaded
        MigrationLoader.LoadContent(source);

        //Write-Host ("    (s--0) {0}" -f $Migration.Name)

        return source;
    }

    private static Migration OnTargetWithoutSource(Migration target)
    {
        //if ($Migration.State -lt 3) {
        //    Write-Host ("    (-t-{1}) {0}" -f $Migration.Name, $Migration.State)
        //}

        return target;
    }

    private Migration OnMatchedSourceAndTarget(Migration source, Migration target)
    {
        // If migration will be applied, ensure its content is loaded
        if (target.State < (int) Phase)
            MigrationLoader.LoadContent(source);

        // Copy source-only properties to target
        target.Path       = source.Path;
        target.HasChanged = target.Hash is not null && target.Hash != source.Hash;
        target.Hash       = source.Hash;
        target.Depends    = source.Depends;
        target.PreSql     = source.PreSql;
        target.CoreSql    = source.CoreSql;
        target.PostSql    = source.PostSql;

        //if ($Migration.State -lt 3 -or $Migration.HasChanged) {
        //    Write-Host (
        //        "    (st{2}{1}) {0}" -f
        //        $Migration.Name,
        //        $Migration.State,
        //        ($Migration.HasChanged ? '!' : '=')
        //    )
        //}

        return target;
    }

    #endregion

    private void Validate(IReadOnlyList<Migration> migrations)
    {
        foreach (var migration in migrations)
        {
            if (migration.State > 0 && migration.HasChanged)
            {
                Cmdlet.WriteWarning(string.Format(
                    "Migration '{0}' has been applied through phase {1} of 3, "  +
                    "but its code in the source directory does not match the "   +
                    "code previously used. To resolve, revert any accidental "   +
                    "changes to this migration. To ignore, update the hash in "  +
                    "the _deploy.Migration table to match the hash of the code " +
                    "in the source directory: {2}.",
                    migration.Name,
                    migration.State,
                    migration.Hash
                ));
            }

            if (migration.State >= (int) Phase)
                continue;

            if (migration.Path is null)
            {
                Cmdlet.WriteWarning(string.Format(
                    "Migration {0} is partially applied (through phase {1} of 3), " +
                    "but no code for it was found in the source directory.  "       +
                    "It is not possible to complete this migration.",
                    migration.Name,
                    migration.State
                ));
            }
        }
    }
}
