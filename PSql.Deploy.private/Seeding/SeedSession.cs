// Copyright 2023 Subatomix Research Inc.
// SPDX-License-Identifier: ISC

namespace PSql.Deploy.Seeding;

/// <summary>
///   A session in which content seeds are applied to a set of target databases.
/// </summary>
public class SeedSession : ISeedSessionControl
{
    /// <summary>
    ///   A factory that creates a <see cref="SeedSession"/> instance.
    /// </summary>
    /// <returns>
    ///   A new seed session.
    /// </returns>
    /// <inheritdoc cref="SeedSession(string, CancellationToken)"/>
    public delegate ISeedSessionControl Factory(string logPath, CancellationToken cancellation);

    /// <summary>
    ///   Gets the default <see cref="SeedSession"/> factory delegate.
    /// </summary>
    public static Factory DefaultFactory { get; } = (p, c) => new SeedSession(p, c);

    /// <summary>
    ///   Initializes a new <see cref="SeedSession"/> instance.
    /// </summary>
    /// <param name="logPath">
    ///   The path of a directory in which to save per-database log files.
    /// </param>
    /// <param name="cancellation">
    ///   The token to monitor for cancellation requests.
    /// </param>
    /// <exception cref="ArgumentNullException">
    ///   <paramref name="logPath"/> is <see langword="null"/>.
    /// </exception>
    public SeedSession(string logPath, CancellationToken cancellation)
    {
        if (logPath is null)
            throw new ArgumentNullException(nameof(logPath));

        Seeds             = ImmutableArray<Seed>.Empty;
        LogPath           = logPath;
        CancellationToken = cancellation;
    }

    /// <inheritdoc/>
    public bool IsWhatIfMode { get; set; }

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

    // Count of applications to target databases that threw exceptions
    private int _errorCount;

    /// <inheritdoc/>
    public void DiscoverSeeds(string path, string[] names)
    {
        Seeds = SeedDiscovery.Get(path, names);
    }

    /// <inheritdoc/>
    public Task ApplyAsync(SqlContextWork work, PSCmdlet cmdlet)
    {
        return Task.CompletedTask;
    }
}
