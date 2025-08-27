// Copyright Subatomix Research Inc.
// SPDX-License-Identifier: MIT

namespace PSql.Deploy.Seeds;

/// <summary>
///   A deployment session in which one or more content seeds are applied to
///   target databases.
/// </summary>
public class SeedSession : DeploymentSession, ISeedSessionInternal
{
    /// <summary>
    ///   Initializes a new <see cref="SeedSession"/> instance.
    /// </summary>
    /// <param name="options">
    ///   The options for the session.
    /// </param>
    /// <param name="console">
    ///   The user interface via which to report progress.
    /// </param>
    /// <exception cref="ArgumentNullException">
    ///   <paramref name="options"/> and/or
    ///   <paramref name="console"/> is <see langword="null"/>.
    /// </exception>
    public SeedSession(SeedSessionOptions options, ISeedConsole console)
        : base(options)
    {
        // options null-checked by base constructor
        ArgumentNullException.ThrowIfNull(console);

        Console = console;
        Defines = options.Defines ?? [];
    }

    /// <summary>
    ///   Gets the user interface via which to report progress.
    /// </summary>
    public ISeedConsole Console { get; }

    /// <summary>
    ///   Gets the preprocessor variable definitions for the session.
    /// </summary>
    public IEnumerable<(string, string)> Defines { get; }

    /// <inheritdoc/>
    public ImmutableArray<Seed> Seeds { get; private set; }
        = ImmutableArray<Seed>.Empty;

    /// <summary>
    ///   Gets or sets the factory for connections to target databases.
    /// </summary>
    /// <remarks>
    ///   This property enables tests to inject a mock connection factory.
    /// </remarks>
    internal SeedTargetConnectionFactory ConnectionFactory { get; set; }
        = (target, logger) => new SqlSeedTargetConnection(target, logger);

    private Task<ImmutableArray<LoadedSeed>>? _loadTask;

    /// <inheritdoc/>
    public void DiscoverSeeds(string path, string[] names)
    {
        Seeds = SeedDiscoverer.Get(path, names);
    }

    /// <inheritdoc/>
    protected override async Task ApplyCoreAsync(Target target, Parallelism parallelism)
    {
        var seeds = await LazyLoadSeedsAsync();

        foreach (var seed in seeds)
            await new SeedApplicator(this, seed, target, parallelism).ApplyAsync();
    }

    [ExcludeFromCodeCoverage(Justification = "timing-dependent")]
    private async Task<ImmutableArray<LoadedSeed>> LazyLoadSeedsAsync()
    {
        if (_loadTask is { } otherTask)
            return await otherTask;

        var deferral = new TaskCompletionSource<ImmutableArray<LoadedSeed>>();

        otherTask = Interlocked.CompareExchange(ref _loadTask, deferral.Task, null);
        if (otherTask is not null)
            return await otherTask;

        try
        {
            var builder = ImmutableArray.CreateBuilder<LoadedSeed>(Seeds.Length);

            foreach (var seed in Seeds)
                builder.Add(SeedLoader.Load(seed, Defines));

            var result = builder.MoveToImmutable();
            deferral.SetResult(result);
            return result;
        }
        catch (Exception e)
        {
            deferral.SetException(e);
            throw;
        }
    }

    private ISeedTargetConnection Connect(Target target, ISqlMessageLogger logger)
    {
        var connection = ConnectionFactory.Invoke(target, logger);

        if (IsWhatIfMode)
            connection = new WhatIfSeedTargetConnection(connection);

        return connection;
    }

    /// <inheritdoc/>
    ISeedTargetConnection ISeedSessionInternal
        .Connect(Target target, ISqlMessageLogger logger)
        => Connect(target, logger);

    /// <inheritdoc/>
    protected override int GetMaxParallelTargets(TargetGroup group)
    {
        // Assume actual parallelism per target equal to group.MaxParallelismPerTarget
        // FUTURE: Consider making this configurable to enable overcommit

        var (targets, remainder) = Math.DivRem(
            group.MaxParallelism,
            group.MaxParallelismPerTarget
        );

        return targets + Math.Sign(remainder);
    }

    /// <inheritdoc/>
    protected override Exception Transform(Exception exception)
    {
        return exception as SeedException
            ?? new SeedException(message: null, exception);
    }
}
