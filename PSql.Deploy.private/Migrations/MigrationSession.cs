// Copyright 2023 Subatomix Research Inc.
// SPDX-License-Identifier: ISC

namespace PSql.Deploy.Migrations;

/// <summary>
///   A session in which seeds are applied to a set of target databases.
/// </summary>
internal class MigrationSession : IMigrationSessionControl, IMigrationSession
{
    /// <summary>
    ///   Initializes a new <see cref="MigrationSession"/> instance.
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
    public MigrationSession(string logPath, CancellationToken cancellation)
    {
        if (logPath is null)
            throw new ArgumentNullException(nameof(logPath));

        Migrations                   = ImmutableArray<Migration>.Empty;
        EarliestDefinedMigrationName = "";

        LogPath              = logPath;
        CancellationToken    = cancellation;
    }

    /// <inheritdoc/>
    public MigrationPhase Phase { get; set; }

    /// <inheritdoc/>
    public bool AllowCorePhase { get; set; }

    /// <inheritdoc/>
    public bool IsWhatIfMode { get; set; }

    /// <inheritdoc/>
    public ImmutableArray<Migration> Migrations { get; private set; }

    /// <inheritdoc/>
    public string EarliestDefinedMigrationName { get; private set; }

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
    public void DiscoverMigrations(string path, string? maxName = null)
    {
        Migrations                   = MigrationRepository.GetAll(path, maxName);
        EarliestDefinedMigrationName = Migrations.FirstOrDefault(m => !m.IsPseudo)?.Name ?? "";
    }

    /// <inheritdoc/>
    public async Task ApplyAsync(SqlContextWork target, PSCmdlet cmdlet)
    {
        using var applicator = new MigrationApplicator(this, target, new MigrationConsole(cmdlet));

        try
        {
            await applicator.ApplyAsync();
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception e)
        {
            Interlocked.Increment(ref _errorCount);
            throw new MigrationException(null, e);
        }
    }

    Task<IReadOnlyList<Migration>> IMigrationSession
        .GetAppliedMigrationsAsync(SqlContext context, ISqlMessageLogger logger)
    {
        return MigrationRepository.GetAllAsync(
            context, EarliestDefinedMigrationName,
            logger, CancellationToken
        );
    }

    /// <inheritdoc/>
    TextWriter IMigrationSession.CreateLog(string fileName)
    {
        Directory.CreateDirectory(LogPath);
        return new StreamWriter(Path.Combine(LogPath, fileName));
    }
}
