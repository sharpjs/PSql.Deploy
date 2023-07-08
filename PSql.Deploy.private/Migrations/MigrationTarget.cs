// Copyright 2023 Subatomix Research Inc.
// SPDX-License-Identifier: ISC

using System.Diagnostics;
using System.Runtime.InteropServices;

namespace PSql.Deploy.Migrations;

using static Environment;
using static RuntimeInformation;

/// <summary>
///   Represents a target database.
/// </summary>
internal class MigrationTarget : IDisposable
{
    /// <summary>
    ///   Initializes a new <see cref="MigrationTarget"/> instance.
    /// </summary>
    /// <param name="engine">
    ///   The migration engine instance.
    /// </param>
    /// <param name="context">
    ///   An object specifying how to connect to the target database.
    /// </param>
    /// <exception cref="ArgumentNullException">
    ///   <paramref name="engine"/> and/or
    ///   <paramref name="context"/> is <see langword="null"/>.
    /// </exception>
    public MigrationTarget(MigrationEngine engine, SqlContext context)
    {
        if (engine is null)
            throw new ArgumentNullException(nameof(engine));
        if (context is null)
            throw new ArgumentNullException(nameof(context));

        _stopwatch   = Stopwatch.StartNew();

        Engine       = engine;

        Context      = context;
        ServerName   = context.AsAzure?.ServerResourceName ?? context.ServerName ?? "local";
        DatabaseName = context.DatabaseName ?? "default";

        LogFileName  = $"{ServerName}.{DatabaseName}.{engine.Phase}.log".SanitizeFileName();
        LogWriter    = new StreamWriter(Path.Combine(engine.LogPath, LogFileName));
        LogConsole   = new TextWriterConsole(LogWriter);
    }

    private readonly Stopwatch _stopwatch;

    public MigrationEngine Engine { get; }

    public ImmutableArray<Migration> DefinedMigrations => Engine.Migrations;
    public string EarliestDefinedMigrationName => Engine.MinimumMigrationName;
    public IConsole Console => Engine.Console;
    public MigrationPhase Phase => Engine.Phase;
    public CancellationToken CancellationToken => Engine.CancellationToken;

    /// <summary>
    ///   Gets an object that specifies how to connect to the target database.
    /// </summary>
    public SqlContext Context { get; }

    /// <summary>
    ///   Gets a display name for the database server.  This name might be a
    ///   DNS name, an Azure resource name, or a placeholder indicating a local
    ///   SQL Server instance.
    /// </summary>
    public string ServerName { get; }

    /// <summary>
    ///   Gets a short name for the database.  This name might be a real
    ///   database name or a placeholder indicating the default database for
    ///   the connection.
    /// </summary>
    public string DatabaseName { get; }

    /// <summary>
    ///   Gets the name of the per-database log file.
    /// </summary>
    public string LogFileName { get; }

    /// <summary>
    ///   Gets a writer that writes to the per-database log file.
    /// </summary>
    public TextWriter LogWriter { get; }

    /// <summary>
    ///   Gets an <see cref="IConsole"/> implementation that writes to the
    ///   per-database log file.
    /// </summary>
    public IConsole LogConsole { get; }

    /// <summary>
    ///   Gets the time that has elapsed since construction of this object.
    /// </summary>
    public TimeSpan ElapsedTime => _stopwatch.Elapsed;

    private int _migrationNameColumnWidth;

    /// <summary>
    ///   Opens a connection to the target database.  The connection will log
    ///   server messages to the per-database log file.
    /// </summary>
    /// <returns>
    ///   An open connection to the target database.
    /// </returns>
    public ISqlConnection Connect()
        => Context.Connect(databaseName: null, LogConsole);

    /// <summary>
    ///   Writes the specified text and a line ending to the per-database log
    ///   file.
    /// </summary>
    /// <param name="text">
    ///   The text to write.
    /// </param>
    public void Log(string text)
        => LogWriter.WriteLine(text);

    /// <inheritdoc/>
    public void Dispose()
        => LogWriter.Dispose();

    /// <summary>
    ///   Applies outstanding migrations to the target database asynchronously.
    /// </summary>
    /// <returns>
    ///   A <see cref="Task"/> representing the asynchronous operation.
    /// </returns>
    public async Task ApplyAsync()
    {
        ReportStarting();

        var appliedMigrations = await GetAppliedMigrations();

        var pendingMigrations = GetPendingMigrations(appliedMigrations);
        if (pendingMigrations.IsEmpty)
            return;

        ValidateMigrations(pendingMigrations); // throws if invalid

        var plan = ComputePlan(pendingMigrations);

        await ExecuteAsync(plan);
    }

    private Task<IReadOnlyList<Migration>> GetAppliedMigrations()
    {
        return MigrationRepository.GetAllAsync(
            Context,    EarliestDefinedMigrationName,
            LogConsole, CancellationToken
        );
    }

    private ImmutableArray<Migration> GetPendingMigrations(IReadOnlyList<Migration> appliedMigrations)
    {
        var pendingMigrations = new MigrationMerger(Phase).Merge(
            DefinedMigrations.AsSpan(),
            appliedMigrations
        );

        if (pendingMigrations.IsEmpty)
            ReportNoPendingMigrations();
        else
            ReportPendingMigrations(pendingMigrations);

        return pendingMigrations;
    }

    private void ValidateMigrations(ImmutableArray<Migration> migrations)
    {
        var isValid = new MigrationValidator(this, Phase, EarliestDefinedMigrationName, Console)
            .Validate(migrations.AsSpan());

        if (!isValid)
            // TODO: custom exception type
            throw new ApplicationException("Unable to perform migrations due to validation errors.");
    }

    private MigrationPlan ComputePlan(ImmutableArray<Migration> migrations)
    {
        var plan = new MigrationPlanner(migrations.AsSpan()).CreatePlan();

        ReportPlan(plan);
        return plan;
    }

    private async Task ExecuteAsync(MigrationPlan plan)
    {
        var items = plan.GetItems(Phase);
        if (!items.Any())
            return;

        using var connection = Connect();
        using var command    = connection.CreateCommand();

        command.CommandTimeout = 0; // No timeout

        var count     = 0;
        var exception = null as Exception;

        try
        {
            foreach (var (migration, phase) in items)
            {
                // TODO
                // // Stop if another thread encountered an error
                // if (_errorCount > 0)
                //     return;

                // Prepare to run the item
                ReportApplying(migration, phase);
                connection.ClearErrors();

                // Run the item
                await ExecuteAsync(migration, phase, command);

                // Report errors if they happened
                connection.ThrowIfHasErrors();
                count++;
            }
        }
        catch (Exception e)
        {
            //Interlocked.Increment(ref _errorCount);
            exception = e;
            throw;
        }
        finally
        {
            ReportApplied(count, exception);
        }
    }

    private Task ExecuteAsync(Migration migration, MigrationPhase phase, ISqlCommand command)
    {
        var sql = migration.GetSql(phase);
        if (sql.IsNullOrEmpty())
            return Task.CompletedTask;

        command.CommandText = sql;

        return command.UnderlyingCommand.ExecuteNonQueryAsync(CancellationToken);
    }

    private void ReportStarting()
    {
        Console.WriteHost(string.Format(
            @"[+{0:hh\:mm\:ss}] {1} {2}:{3} Starting",
            /*{0}*/ Engine._totalTime.Elapsed,
            /*{1}*/ Phase.ToFixedWidthString(),
            /*{2}*/ DatabaseName,
            /*{3}*/ Space.Pad(DatabaseName, Engine._databaseNameColumnWidth)
        ));

        Log("PSql.Deploy Migration Log");
        Log("");
        Log($"Migration Phase:    {Phase}");
        Log($"Target Server:      {ServerName}");
        Log($"Target Database:    {DatabaseName}");
        Log($"Start Time:         {DateTime.UtcNow:o}");
        Log($"Machine:            {MachineName}");
        Log($"Logical CPUs:       {ProcessorCount}");
        Log($"User:               {UserName}");
        Log($"Process:            {Process.GetCurrentProcess().Id} ({ProcessArchitecture})");
        Log($"Operating System:   {OSDescription} ({OSArchitecture})");
        Log($".NET Runtime:       {FrameworkDescription}");
    }

    private void ReportNoPendingMigrations()
    {
        Log("Migrations:         0");
        Log("");
        Log("Nothing to do.");
    }

    private void ReportPendingMigrations(ImmutableArray<Migration> migrations)
    {
        // NAME             FILES     PROGRESS          DEPENDS-ON
        // 2023-01-01-123   Ok        (new)             (none)
        // 2023-01-02-234   Missing   Pre->Core->Post   (none)
        // 2023-01-03-345   Ok        Pre->Core         2023-01-01-123
        // 2023-01-04-456   Changed   Pre               (none)

        _migrationNameColumnWidth = ComputeMigrationNameColumnWidth(migrations);

        Log($"Migrations:         {migrations.Length}");
        Log("");

        Log(string.Format(
            "NAME{0}   FILES     PROGRESS          DEPENDS-ON",
            Space.Pad("NAME", _migrationNameColumnWidth)
        ));

        foreach (var migration in migrations)
        {
            if (migration.IsPseudo)
                continue;

            Log(string.Format(
                "{0}{1}   {2}   {3}   {4}",
                /*{0}*/ migration.Name,
                /*{1}*/ Space.Pad(migration.Name, _migrationNameColumnWidth),
                /*{2}*/ migration.GetFixedWithFileStatusString(),
                /*{3}*/ migration.State.ToFixedWidthString(),
                /*{4}*/ migration.Depends?.LastOrDefault() ?? "(none)"
            ));
        }

        Log("");
    }

    private void ReportPlan(MigrationPlan plan)
    {
        Log("Sequence:");
        Log("");

        var items = plan.GetItems(Phase);

        Log(string.Format(
            "NAME{0}   PHASE",
            Space.Pad("NAME", _migrationNameColumnWidth)
        ));

        foreach (var (migration, phase) in items)
            Log(string.Format(
                "{0}{1}   {2}",
                /*{0}*/ migration.Name,
                /*{1}*/ Space.Pad(migration.Name, _migrationNameColumnWidth),
                /*{2}*/ phase
            ));

        Log("");
    }

    private void ReportApplying(Migration migration, MigrationPhase phase)
    {
        Console.WriteHost(string.Format(
            @"[+{0:hh\:mm\:ss}] {1} {2}:{3} Applying {4} {5}",
            /*{0}*/ Engine._totalTime.Elapsed,
            /*{1}*/ Phase.ToFixedWidthString(),
            /*{2}*/ DatabaseName,
            /*{3}*/ Space.Pad(DatabaseName, Engine._databaseNameColumnWidth),
            /*{4}*/ migration.Name,
            /*{5}*/ phase
        ));
    }

    private void ReportApplied(int count, Exception? exception)
    {
        var abnormality = 
            exception is not null ? " [EXCEPTION]"  :
            //_errorCount > 0       ? " [INCOMPLETE]" :
            null;

        Console.WriteHost(string.Format(
            @"[+{0:hh\:mm\:ss}] {1} {2}:{3} Applied {4} migration(s) in {5:N3} second(s){6}",
            /*{0}*/ Engine._totalTime.Elapsed,
            /*{1}*/ Phase.ToFixedWidthString(),
            /*{2}*/ DatabaseName,
            /*{3}*/ Space.Pad(DatabaseName, Engine._databaseNameColumnWidth),
            /*{4}*/ count,
            /*{5}*/ ElapsedTime.TotalSeconds,
            /*{6}*/ abnormality
        ));
    }

    private static int ComputeMigrationNameColumnWidth(ImmutableArray<Migration> migrations)
    {
        const int HeaderLength = 4; // "NAME".Length

        return Math.Max(HeaderLength, migrations.Max(m => m.Name.Length));
    }
}
