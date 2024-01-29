// Copyright 2024 Subatomix Research Inc.
// SPDX-License-Identifier: ISC

using System.Diagnostics;
using System.Runtime.InteropServices;

namespace PSql.Deploy.Migrations;

using static Environment;
using static RuntimeInformation;

/// <summary>
///   An algorithm that applies schema migrations a target database.
/// </summary>
internal class MigrationApplicator : IMigrationValidationContext, IDisposable
{
    // The target of this applicator
    private readonly SqlContextWork _target;

    // Time elapsed since construction
    private readonly Stopwatch _stopwatch;

    /// <summary>
    ///   Initializes a new <see cref="MigrationApplicator"/> instance.
    /// </summary>
    /// <param name="session">
    ///   The migration session.
    /// </param>
    /// <param name="target">
    ///   An object specifying how to connect to the target database.
    /// </param>
    /// <exception cref="ArgumentNullException">
    ///   <paramref name="session"/> and/or
    ///   <paramref name="target"/> is <see langword="null"/>.
    /// </exception>
    public MigrationApplicator(IMigrationSession session, SqlContextWork target)
    {
        if (session is null)
            throw new ArgumentNullException(nameof(session));
        if (target is null)
            throw new ArgumentNullException(nameof(target));

        Session          = session;
        _target          = target;

        LogWriter        = session.CreateLog(target);
        SqlMessageLogger = new TextWriterSqlMessageLogger(LogWriter);

        _stopwatch       = Stopwatch.StartNew();
    }

    /// <summary>
    ///   Gets the migration session.
    /// </summary>
    public IMigrationSession Session { get; }

    /// <inheritdoc cref="IMigrationSession.Console"/>
    public IMigrationConsole Console => Session.Console;

    /// <inheritdoc cref="IMigrationSession.EarliestDefinedMigrationName"/>
    public string EarliestDefinedMigrationName => Session.EarliestDefinedMigrationName;

    /// <inheritdoc/>
    public MigrationPhase Phase => Session.Phase;

    /// <inheritdoc cref="IMigrationSession.CancellationToken"/>
    public CancellationToken CancellationToken => Session.CancellationToken;

    internal IMigrationInternals Internals { get; set; } = MigrationInternals.Instance;

    /// <summary>
    ///   Gets an object that specifies how to connect to the target database.
    /// </summary>
    public SqlContext Context => _target.Context;

    /// <inheritdoc/>
    public string ServerName => _target.ServerDisplayName;

    /// <inheritdoc/>
    public string DatabaseName => _target.DatabaseDisplayName;

    /// <summary>
    ///   Gets a writer that writes to the per-database log.
    /// </summary>
    public TextWriter LogWriter { get; }

    /// <summary>
    ///   Gets an <see cref="ISqlMessageLogger"/> implementation that writes to
    ///   the per-database log.
    /// </summary>
    public ISqlMessageLogger SqlMessageLogger { get; }

    /// <summary>
    ///   Gets or sets whether the object allows a non-skippable <c>Core</c>
    ///   phase to exist.  The default is <see langword="false"/>.
    /// </summary>
    public bool AllowCorePhase => Session.AllowCorePhase;

    /// <summary>
    ///   Gets or sets whether the object operates in what-if mode.  In this
    ///   mode, the object reports what actions it would perform against the
    ///   target database but does not perform the actions.
    /// </summary>
    public bool IsWhatIfMode => Session.IsWhatIfMode;

    // Dynamic column width
    private int _migrationNameMaxLength;

    // Count of migrations successfully applied
    private int _appliedCount;

    // Outcome
    private TargetDisposition _disposition;

    /// <summary>
    ///   Applies outstanding migrations to the target database asynchronously.
    /// </summary>
    /// <returns>
    ///   A <see cref="Task"/> representing the asynchronous operation.
    /// </returns>
    // /// <exception cref="MigrationValidationException">
    // ///   One or more validation errors was reported.
    // /// </exception>
    public async Task ApplyAsync()
    {
        try
        {
            ReportStarting();

            var appliedMigrations = await GetAppliedMigrationsAsync();
            var pendingMigrations = GetPendingMigrations(appliedMigrations);
            var plan              = ComputeMigrationPlan(pendingMigrations);

            if (!Validate(plan))
                return; // TODO: throw?

            await ExecuteAsync(plan);
        }
        catch (OperationCanceledException)
        {
            _disposition = TargetDisposition.Incomplete;
            throw;
        }
        catch (Exception e)
        {
            _disposition = TargetDisposition.Failed;
            ReportException(e);
            throw;
        }
        finally
        {
            ReportEnded();
        }
    }

    private Task<IReadOnlyList<Migration>> GetAppliedMigrationsAsync()
    {
        return Session.GetAppliedMigrationsAsync(Context, SqlMessageLogger);
    }

    private ImmutableArray<Migration> GetPendingMigrations(IReadOnlyList<Migration> appliedMigrations)
    {
        var pendingMigrations = new MigrationMerger(Internals).Merge(
            definedMigrations: Session.Migrations.AsSpan(),
            appliedMigrations
        );

        MigrationReferenceResolver.Resolve(pendingMigrations.AsSpan());

        return pendingMigrations;
    }

    private MigrationPlan ComputeMigrationPlan(ImmutableArray<Migration> pendingMigrations)
    {
        return new MigrationPlanner(pendingMigrations).CreatePlan();
    }

    private bool Validate(MigrationPlan plan)
    {
        if (plan.PendingMigrations.IsEmpty)
        {
            ReportNoPendingMigrations();
            return false;
        }

        var valid = new MigrationValidator(this).Validate(plan);

        ReportPendingMigrations(plan);
        ReportDiagnostics      (plan.PendingMigrations);

        if (!valid)
        {
            // Nothing extra to report
            return false;
        }

        if (plan.IsEmpty(Phase))
        {
            ReportEmptyPlan();
            return false;
        }

        if (!AllowCorePhase && plan.IsCoreRequired)
        {
            ReportCoreRequired();
            return false;
        }

        return true;
    }

    private async Task ExecuteAsync(MigrationPlan plan)
    {
        ReportApplying();

        if (IsWhatIfMode)
            return;

        using var connection = Internals.Connect(Context, SqlMessageLogger);
        using var command    = connection.CreateCommand();

        command.CommandTimeout = 0; // No timeout

        foreach (var (migration, phase) in plan.GetItems(Phase))
        {
            // Prepare to run the item
            ReportApplying(migration, phase);
            connection.ClearErrors();

            // Run the item
            await ExecuteAsync(migration, phase, command);

            // Report errors or mark applied
            connection.ThrowIfHasErrors();
            _appliedCount++;
        }
    }

    private Task ExecuteAsync(Migration migration, MigrationPhase phase, ISqlCommand command)
    {
        var sql = migration[phase].Sql;
        if (sql.IsNullOrEmpty())
            return Task.CompletedTask;

        command.CommandText = sql;

        return command.UnderlyingCommand.ExecuteNonQueryAsync(CancellationToken);
    }

    private void ReportStarting()
    {
        Console.ReportStarting();

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

    private void ReportPendingMigrations(MigrationPlan plan)
    {
        // A contrived example showing nearly all combinations of output:
        //
        // Pending Migrations: 4
        //
        //                   PENDING MIGRATIONS                         PHASE PLAN
        //                                                         ┌─────┬──────┬──────┐
        // NAME            CHECK    PROGRESS       DEPENDS-ON      │ PRE │ CORE │ POST │
        // ════════════════════════════════════════════════════════╪═════╪══════╪══════╡
        // 2023-01-01-123  Ok       (new)          (none)          │ Pre │ Core │ Post │
        // 2023-01-02-234  Missing  Pre>Core>Post  (none)          │     │      │      │
        // 2023-01-03-345  Invalid  Pre>Core       2023-01-01-123  │     │      │ Post │
        // 2023-01-04-456  Changed  Pre            (none)          │     │ Core │ Post │
        //                                                         └─────┴──────┴──────┘

        const int
            NameHeaderWidth      =  4, // NAME
            DependsOnHeaderWidth = 10; // DEPENDS-ON

        var migrations = plan.PendingMigrations;

        // Dynamic column widths
        _migrationNameMaxLength  = migrations.Max(m => m.Name.Length);

        var nameColumnWidth      = Math.Max(NameHeaderWidth,      _migrationNameMaxLength);
        var dependsOnColumnWidth = Math.Max(DependsOnHeaderWidth, _migrationNameMaxLength);

        var namePad       = Space.Pad("NAME",       nameColumnWidth);
        var dependsOnPad  = Space.Pad("DEPENDS-ON", dependsOnColumnWidth);
        var pendingPad    = Space.Get(namePad.Length + dependsOnPad.Length);

        var preInCorePad  = Space.Get(plan.HasPreContentInCore  ? 4 : 0); // align  Pre>
        var postInCorePad = Space.Get(plan.HasPostContentInCore ? 5 : 0); // align >Post
        var coreBorder    = new string('─', preInCorePad.Length + postInCorePad.Length);

        // Header
        Log("");
        Log($"Pending Migrations: {migrations.Length}");
        Log("");

        // Table header row 1
        {
            var (pendingPadL, pendingPadR) = Space.GetCentering(pendingPad.Length);
            var (phasesPadL,  phasesPadR)  = Space.GetCentering(coreBorder.Length);

            Log(string.Format(
                "           {0}PENDING MIGRATIONS{1}                  {2}PHASE PLAN{3}",
                pendingPadL, pendingPadR, phasesPadL, phasesPadR
            ));
        }

        // Table header row 2
        Log(string.Format(
            "    {0}                                      ┌─────┬───{1}───┬──────┐",
            pendingPad, coreBorder
        ));

        // Table header row 3
        Log(string.Format(
            "NAME{0}  CHECK    PROGRESS       DEPENDS-ON{1}  │ PRE │ {2}CORE{3} │ POST │",
            namePad, dependsOnPad, preInCorePad, postInCorePad
        ));

        // Table header row 4
        Log(string.Format("════{0}══════════════════════════════════════╪═════╪═══{1}═══╪══════╡",
            new string('═', pendingPad.Length),
            new string('═', coreBorder.Length)
        ));

        // Body
        foreach (var migration in migrations)
        {
            if (migration.IsPseudo)
                continue;

            var preInPre   = migration.Pre .PlannedPhase is MigrationPhase.Pre;
            var preInCore  = migration.Pre .PlannedPhase is MigrationPhase.Core;
            var coreInCore = migration.Core.PlannedPhase is MigrationPhase.Core;
            var postInCore = migration.Post.PlannedPhase is MigrationPhase.Core;
            var postInPost = migration.Post.PlannedPhase is MigrationPhase.Post;
            var isPlanned  = preInPre | preInCore | coreInCore | postInCore | postInPost;

            if (!isPlanned)
                continue;

            // TODO: Skip applied
            var dependsOn = migration.DependsOn.LastOrDefault()?.Name ?? "(none)";

            Log(string.Format(
                "{0}{1}  {2}  {3}  {4}{5}  │ {6} │ {7}{8}{9} │ {10} │",
                /*{ 0}*/ migration.Name,
                /*{ 1}*/ Space.Pad(migration.Name, nameColumnWidth),
                /*{ 2}*/ migration.GetFixedWidthStatusString(),
                /*{ 3}*/ migration.State.ToFixedWidthString(),
                /*{ 4}*/ dependsOn,
                /*{ 5}*/ Space.Pad(dependsOn, dependsOnColumnWidth),
                /*{ 6}*/ preInPre   ?  "Pre"  : Space.Get(3),
                /*{ 7}*/ preInCore  ?  "Pre>" : preInCorePad,
                /*{ 8}*/ coreInCore ?  "Core" : Space.Get(4),
                /*{ 9}*/ postInCore ? ">Post" : postInCorePad,
                /*{10}*/ postInPost ?  "Post" : Space.Get(4)
            ));
        }

        // Footer
        Log(string.Format(
            "    {0}                                      └─────┴───{1}───┴──────┘",
            pendingPad, coreBorder
        ));
    }

    private void ReportDiagnostics(ImmutableArray<Migration> pendingMigrations)
    {
        // Header
        Log("");
        Log("Validation Results:");
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
            Log("All pending migrations are valid for the current phase.");
    }

    private void ReportDiagnostic(MigrationDiagnostic diagnostic)
    {
        Console.ReportProblem(diagnostic.Message);

        Log(string.Concat(
            diagnostic.IsError ? "Error: " : "Warning: ",
            diagnostic.Message
        ));
    }

    private void ReportEmptyPlan()
    {
        Log("");
        Log("Migration Sequence:");
        Log("");
        Log("Nothing to do for the current phase.");
    }

    private void ReportCoreRequired()
    {
        var message = string.Format(
            "One or more migration(s) to be applied to database [{0}].[{1}] "  +
            "requires the Core (downtime) phase, but the -AllowCorePhase "     +
            "switch was not present for the Invoke-SqlMigrations command.  "   +
            "To allow the Core phase, pass the switch to the command.  "       +
            "Otherwise, ensure that all migrations begin with a '--# PRE' or " +
            "'--# POST' directive and that any '--# REQUIRES:' directives "    +
            "reference only migrations that have been completely applied.",
            ServerName,
            DatabaseName
        );

        Console.ReportProblem(message);

        Log("");
        Log("Error: " + message);
    }

    private void ReportApplying()
    {
        Log("");
        Log("Execution Log:");
        Log("");
    }

    private void ReportApplying(Migration migration, MigrationPhase phase)
    {
        Console.ReportApplying(migration.Name, phase);

        Log(string.Concat("[", migration.Name, " ", phase.ToString(), "]"));

        // Server messages appear here
    }

    private void ReportException(Exception exception)
    {
        Console.ReportProblem(exception.Message);

        Log(exception.ToString());
    }

    private void ReportEnded()
    {
        var elapsed = _stopwatch.Elapsed;

        Console.ReportApplied(_appliedCount, elapsed, _disposition);

        // Footer
        Log("");
        Log(string.Format(
            "Applied {0} migration(s) in {1:N3} second(s)",
            _appliedCount,
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
