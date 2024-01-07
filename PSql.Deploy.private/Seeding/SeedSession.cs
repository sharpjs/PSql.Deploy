// Copyright 2023 Subatomix Research Inc.
// SPDX-License-Identifier: ISC

namespace PSql.Deploy.Seeding;

/// <summary>
///   A session in which content seeds are applied to a set of target databases.
/// </summary>
public class SeedSession : ISeedSessionControl, ISeedSession
{
    /// <summary>
    ///   A factory that creates a <see cref="SeedSession"/> instance.
    /// </summary>
    /// <returns>
    ///   A new seed session.
    /// </returns>
    /// <inheritdoc cref="SeedSession(ISeedConsole, string, CancellationToken)"/>
    public delegate ISeedSessionControl Factory(
        ISeedConsole      console,
        string            logPath,
        CancellationToken cancellation
    );

    /// <summary>
    ///   Gets the default <see cref="SeedSession"/> factory delegate.
    /// </summary>
    public static Factory DefaultFactory { get; } = (c, p, t) => new SeedSession(c, p, t);

    /// <summary>
    ///   Initializes a new <see cref="SeedSession"/> instance.
    /// </summary>
    /// <param name="console">
    ///   The console on which to report the progress of seed application to a
    ///   particular target database.
    /// </param>
    /// <param name="logPath">
    ///   The path of a directory in which to save per-database log files.
    /// </param>
    /// <param name="cancellation">
    ///   The token to monitor for cancellation requests.
    /// </param>
    /// <exception cref="ArgumentNullException">
    ///   <paramref name="logPath"/> is <see langword="null"/>.
    /// </exception>
    public SeedSession(ISeedConsole console, string logPath, CancellationToken cancellation)
    {
        if (console is null)
            throw new ArgumentNullException(nameof(console));
        if (logPath is null)
            throw new ArgumentNullException(nameof(logPath));

        Seeds             = ImmutableArray<Seed>.Empty;
        Console           = console;
        LogPath           = logPath;
        CancellationToken = cancellation;
    }

    /// <inheritdoc/>
    public bool IsWhatIfMode { get; set; }

    /// <inheritdoc/>
    public int MaxParallelism { get; set; }

    /// <inheritdoc/>
    public ImmutableArray<Seed> Seeds { get; private set; }

    /// <summary>
    ///   Gets the path of a directory in which to save log files.
    /// </summary>
    public string LogPath { get; }

    /// <inheritdoc/>
    public CancellationToken CancellationToken { get; }

    /// <inheritdoc/>
    public bool HasErrors => Volatile.Read(ref _errorCount) > 0;

    /// <inheritdoc/>
    public ISeedConsole Console { get; }

    // Count of applications to target databases that threw exceptions
    private int _errorCount;

    /// <inheritdoc/>
    public void DiscoverSeeds(string path, string[] names)
    {
        Seeds = SeedDiscovery.Get(path, names);
    }

    /// <inheritdoc/>
    public async Task ApplyAsync(SqlContextWork target)
    {
        try
        {
            foreach (var seed in Seeds)
            {
                var loadedSeed = SeedLoader.Load(seed); // TODO: once
                using (var applicator = new SeedApplicator(this, loadedSeed, target))
                    await applicator.ApplyAsync();
            }
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception e)
        {
            Interlocked.Increment(ref _errorCount);
            throw new SeedException(null, e);
        }
    }

    /// <inheritdoc/>
    TextWriter ISeedSession.CreateLog(Seed seed, SqlContextWork target)
    {
        if (seed is null)
            throw new ArgumentNullException(nameof(seed));
        if (target is null)
            throw new ArgumentNullException(nameof(target));

        Directory.CreateDirectory(LogPath);

        var server   = target.ServerDisplayName;
        var database = target.DatabaseDisplayName;
        var fileName = $"{server}.{database}.Seed_{seed.Name}.log".SanitizeFileName();

        return new StreamWriter(Path.Combine(LogPath, fileName));
    }
}
