// Copyright 2024 Subatomix Research Inc.
// SPDX-License-Identifier: ISC

namespace PSql.Deploy.Migrations;

/// <summary>
///   A session in which schema migrations are applied to a set of target
///   databases.
/// </summary>
internal class MigrationSession : IMigrationSessionControl, IMigrationSession
{
    /// <summary>
    ///   Initializes a new <see cref="MigrationSession"/> instance.
    /// </summary>
    /// <inheritdoc cref="MigrationSessionFactory.Create"/>
    public MigrationSession(
        IMigrationConsole console,
        string            logPath,
        CancellationToken cancellation)
    {
        if (console is null)
            throw new ArgumentNullException(nameof(console));
        if (logPath is null)
            throw new ArgumentNullException(nameof(logPath));

        Migrations                   = ImmutableArray<Migration>.Empty;
        EarliestDefinedMigrationName = "";

        Console              = console;
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

    /// <inheritdoc/>
    public IMigrationConsole Console { get; }

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
        using var applicator = new MigrationApplicator(this, target);

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
    TextWriter IMigrationSession.CreateLog(SqlContextWork target)
    {
        if (target is null)
            throw new ArgumentNullException(nameof(target));

        Directory.CreateDirectory(LogPath);

        var server   = target.ServerDisplayName;
        var database = target.DatabaseDisplayName;
        var fileName = $"{server}.{database}.{(int) Phase}_{Phase}.log".SanitizeFileName();

        return new StreamWriter(Path.Combine(LogPath, fileName));
    }
}
