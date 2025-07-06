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
    ///   Options for the session.
    /// </param>
    /// <param name="console">
    ///   The user interface via which to report progress.
    /// </param>
    /// <param name="maxErrorCount">
    ///   The maximum count of exceptions that the session should tolerate
    ///   before cancelling ongoing operations.  Must be zero or a positive
    ///   number.
    /// </param>
    /// <exception cref="ArgumentNullException">
    ///   <paramref name="console"/> is <see langword="null"/>.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    ///   <paramref name="maxErrorCount"/> is negative.
    /// </exception>
    public SeedSession(SeedSessionOptions options, ISeedConsole console,
        IEnumerable<(string, string)>? defines = null,
        int maxErrorCount = 0)
        : base(maxErrorCount)
    {
        if (console is null)
            throw new ArgumentNullException(nameof(console));

        Options = options;
        Console = console;
        Defines = defines ?? [];
    }

    /// <summary>
    ///   Gets thie options for the session.
    /// </summary>
    public SeedSessionOptions Options { get; }

    /// <summary>
    ///   Gets the user interface via which to report progress.
    /// </summary>
    public ISeedConsole Console { get; }

    /// <summary>
    ///   Gets the preprocessor variable definitions for the session.
    /// </summary>
    public IEnumerable<(string, string)> Defines { get; }

    /// <inheritdoc/>
    public override bool IsWhatIfMode
        => (Options & SeedSessionOptions.IsWhatIfMode) is not 0;

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
    protected override async Task ApplyCoreAsync(Target target, int maxParallelism)
    {
        var seeds = await LazyLoadSeedsAsync();

        foreach (var seed in seeds)
            await new SeedApplicator(this, seed, target).ApplyAsync();
    }

    private async Task<ImmutableArray<LoadedSeed>> LazyLoadSeedsAsync()
    {
        if (_loadTask is { } otherTask)
            return await otherTask;

        var deferral = new TaskCompletionSource<ImmutableArray<LoadedSeed>>();

        otherTask = Interlocked.CompareExchange(ref _loadTask, deferral.Task, null);
        if (otherTask is not null)
            return await otherTask;

        var builder = ImmutableArray.CreateBuilder<LoadedSeed>();

        foreach (var seed in Seeds)
            builder.Add(SeedLoader.Load(seed, Defines));

        var result = builder.MoveToImmutable();
        deferral.SetResult(result);
        return result;
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
    protected override Exception Transform(Exception exception)
    {
        return exception as SeedException
            ?? new SeedException(message: null, exception);
    }
}
