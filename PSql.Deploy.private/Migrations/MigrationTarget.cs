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
internal class MigrationTarget : IMigrationValidationContext, IDisposable
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

    /// <summary>
    ///   Gets the engine for which this instance works.
    /// </summary>
    public MigrationEngine Engine { get; }

    /// <inheritdoc cref="MigrationEngine.MinimumMigrationName"/>
    public string EarliestDefinedMigrationName => Engine.MinimumMigrationName;

    /// <inheritdoc/>
    public MigrationPhase Phase => Engine.Phase;

    /// <inheritdoc cref="MigrationEngine.CancellationToken"/>
    public CancellationToken CancellationToken => Engine.CancellationToken;

    /// <summary>
    ///   Gets an object that specifies how to connect to the target database.
    /// </summary>
    public SqlContext Context { get; }

    /// <inheritdoc/>
    public string ServerName { get; }

    /// <inheritdoc/>
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

    // Time elapsed since construction
    private readonly Stopwatch _stopwatch;

    // For report tabulation
    private int _migrationNameMaxLength;

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

        Validate(pendingMigrations); // throws if invalid

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
        return new MigrationMerger(Phase).Merge(
            definedMigrations: Engine.Migrations.AsSpan(),
            appliedMigrations
        );
    }

    private void Validate(ImmutableArray<Migration> pendingMigrations)
    {
        var valid = new MigrationValidator(this).Validate(pendingMigrations.AsSpan());

        if (pendingMigrations.IsEmpty)
            ReportNoPendingMigrations();
        else
            ReportPendingMigrations(pendingMigrations);

        ReportDiagnostics(pendingMigrations);

        if (!valid)
            throw new MigrationValidationException();
    }

    private MigrationPlan ComputePlan(ImmutableArray<Migration> pendingMigrations)
    {
        var plan = new MigrationPlanner(pendingMigrations.AsSpan()).CreatePlan();

        ReportPlan(plan);

        return plan;
    }

    private async Task ExecuteAsync(MigrationPlan plan)
    {
        var items = plan.GetItems(Phase);
        if (!items.Any())
            return;

        using var connection = Context.Connect(databaseName: null, LogConsole);
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
        Engine.ReportStarting(DatabaseName);

        var process = Process.GetCurrentProcess();

        Log("PSql.Deploy Migration Log");
        Log("");
        Log($"Migration Phase:    {Phase}");
        Log($"Target Server:      {ServerName}");
        Log($"Target Database:    {DatabaseName}");
        Log($"Start Time:         {DateTime.UtcNow:o}");
        Log($"Machine:            {MachineName}");
        Log($"Logical CPUs:       {ProcessorCount}");
        Log($"User:               {UserName}");
        Log($"Process:            {process.Id} {process.ProcessName} ({ProcessArchitecture})");
        Log($"Operating System:   {OSDescription} ({OSArchitecture})");
        Log($".NET Runtime:       {FrameworkDescription}");
    }

    private void ReportNoPendingMigrations()
    {
        Log("");
        Log("Pending Migrations: 0");
        Log("");
        Log("Nothing to do.");
    }

    private void ReportPendingMigrations(ImmutableArray<Migration> migrations)
    {
        // A contrived example showing all forms of output:
        //
        // Pending Migrations: 4
        //
        // NAME             FILES     PROGRESS          DEPENDS-ON
        // 2023-01-01-123   Ok        (new)             (none)
        // 2023-01-02-234   Missing   Pre->Core->Post   (none)
        // 2023-01-03-345   Ok        Pre->Core         2023-01-01-123
        // 2023-01-04-456   Changed   Pre               (none)           **ERROR**

        const int
            NameHeaderWidth      =  4, // NAME
            DependsOnHeaderWidth = 10; // DEPENDS-ON

        // Dynamic column widths
        _migrationNameMaxLength = migrations.Max(m => m.Name.Length);
        var nameColumnWidth      = Math.Max(NameHeaderWidth,      _migrationNameMaxLength);
        var dependsOnColumnWidth = Math.Max(DependsOnHeaderWidth, _migrationNameMaxLength);

        // Header
        Log("");
        Log($"Pending Migrations: {migrations.Length}");
        Log("");
        Log(string.Format(
            "NAME{0}   FILES     PROGRESS          DEPENDS-ON",
            Space.Pad("NAME", nameColumnWidth)
        ));

        // Body
        foreach (var migration in migrations)
        {
            if (migration.IsPseudo)
                continue;

            var marker    = GetDiagnosticMarker(migration.Diagnostics);
            var dependsOn = migration.Depends?.LastOrDefault() ?? "(none)";

            var dependsOnPad = marker is not null
                ? Space.Pad(dependsOn, dependsOnColumnWidth)
                : null;

            Log(string.Format(
                "{0}{1}   {2}   {3}   {4}{5}{6}",
                /*{0}*/ migration.Name,
                /*{1}*/ Space.Pad(migration.Name, nameColumnWidth),
                /*{2}*/ migration.GetFixedWithFileStatusString(),
                /*{3}*/ migration.State.ToFixedWidthString(),
                /*{4}*/ dependsOn,
                /*{5}*/ dependsOnPad,
                /*{6}*/ marker
            ));
        }
    }

    private static string? GetDiagnosticMarker(IReadOnlyList<MigrationDiagnostic> diagnostics)
    {
        if (diagnostics.Count == 0)
            return null;

        foreach (var diagnostic in diagnostics)
            if (diagnostic.IsError)
                return "**ERROR**";

        return "**WARNING**";
    }

    private void ReportDiagnostics(ImmutableArray<Migration> pendingMigrations)
    {
        // Header
        Log("");
        Log("Validation:");
        Log("");

        var hasDiagnostics = false;

        // Body
        foreach (var migration in pendingMigrations)
        {
            if (migration.Diagnostics.Count == 0)
                continue;

            hasDiagnostics = true;

            foreach (var diagnostic in migration.Diagnostics)
                ReportDiagnostic(diagnostic);
        }

        if (!hasDiagnostics)
            Log("All pending migrations are valid.");
    }

    private void ReportDiagnostic(MigrationDiagnostic diagnostic)
    {
        Log(string.Concat(
            diagnostic.IsError ? "Error: " : "Warning: ",
            diagnostic.Message
        ));
    }

    private void ReportPlan(MigrationPlan plan)
    {
        const int
            NameHeaderWidth =  4; // NAME

        // Dynamic column widths
        var nameColumnWidth = Math.Max(NameHeaderWidth, _migrationNameMaxLength);

        // Header
        Log("");
        Log("Sequence:");
        Log("");
        Log(string.Format(
            "NAME{0}   PHASE",
            Space.Pad("NAME", nameColumnWidth)
        ));

        // Body
        foreach (var (migration, phase) in plan.GetItems(Phase))
        {
            Log(string.Format(
                "{0}{1}   {2}",
                /*{0}*/ migration.Name,
                /*{1}*/ Space.Pad(migration.Name, nameColumnWidth),
                /*{2}*/ phase
            ));
        }

        // Separator between table and migration script output
        Log("");
    }

    private void ReportApplying(Migration migration, MigrationPhase phase)
    {
        Engine.ReportApplying(DatabaseName, migration.Name, phase);

        Log(string.Concat("[", migration.Name, " ", phase.ToString(), "]"));

        // Server messages appear here
    }

    private void ReportApplied(int count, Exception? exception)
    {
        var elapsed = _stopwatch.Elapsed;

        Engine.ReportApplied(DatabaseName, count, elapsed, exception);

        // Exception
        if (exception is not null)
            Log(exception.ToString());

        // Footer
        Log("");
        Log(string.Format(
            "Applied {0} migration(s) in {1:N3} second(s)",
            count,
            elapsed.TotalSeconds
        ));
    }

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
}
