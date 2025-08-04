// iopyright Subatomix Research Inc.
// SPDX-License-Identifier: MIT

using System.Diagnostics;

namespace PSql.Deploy.Migrations;

/// <summary>
///   Core logic for applying schema migrations to a target database.
/// </summary>
internal class MigrationApplicator : IMigrationApplication
{
    /// <summary>
    ///   Initializes a new <see cref="MigrationApplicator"/> instance for the
    ///   specified session and target database.
    /// </summary>
    /// <param name="session">
    ///   The migration session.
    /// </param>
    /// <param name="target">
    ///   An object representing the target database.
    /// </param>
    /// <exception cref="ArgumentNullException">
    ///   <paramref name="session"/> and/or
    ///   <paramref name="target"/> is <see langword="null"/>.
    /// </exception>
    public MigrationApplicator(IMigrationSessionInternal session, Target target)
    {
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(target);

        Session = session;
        Target  = target;

        _stopwatch = new();
    }

    /// <inheritdoc cref="IMigrationApplication.Session"/>
    public IMigrationSessionInternal Session { get; }

    /// <inheritdoc/>
    IMigrationSession IMigrationApplication.Session => Session;

    /// <summary>
    ///   Gets the user interface via which to report progress.
    /// </summary>
    public IMigrationConsole Console => Session.Console;

    /// <summary>
    ///   Gets an object representing the target database.
    /// </summary>
    public Target Target { get; }

    // Time elapsed in ApplyAsync
    private readonly Stopwatch _stopwatch;

    // Dynamic column width
    private int _migrationNameMaxLength;

    // Count of migrations successfully applied
    private int _appliedCount;

    // Outcome
    private TargetDisposition _disposition;

    // Log
    private TextWriter? _logWriter;

    /// <summary>
    ///   Applies migrations to the target database asynchronously.
    /// </summary>
    /// <returns>
    ///   A <see cref="Task"/> representing the asynchronous operation.
    /// </returns>
    public async Task ApplyAsync()
    {
        BeginLog();

        try
        {
            ReportStarting();

            await using var connection = Connect();

            await connection.OpenAsync(Session.CancellationToken);

            var appliedMigrations = await GetAppliedMigrationsAsync(connection);
            var pendingMigrations = GetPendingMigrations(appliedMigrations);
            var migrationPlan     = ComputeMigrationPlan(pendingMigrations);

            if (ShouldExecute(migrationPlan))
                await ExecuteAsync(migrationPlan, connection);
        }
        catch (OperationCanceledException)
        {
            _disposition = TargetDisposition.Incomplete;
            ReportCanceled();
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
                  BestEffort.Do     (self => self.ReportEnded(),   this);
            await BestEffort.DoAsync(self => self.CloseLogAsync(), this);
        }
    }

    private IMigrationTargetConnection Connect()
    {
        Assume.NotNull(_logWriter);

        var logger = new TextWriterSqlMessageLogger(_logWriter);

        return Session.Connect(Target, logger);
    }

    private Task<IReadOnlyList<Migration>> GetAppliedMigrationsAsync(IMigrationTargetConnection connection)
    {
        return connection.GetAppliedMigrationsAsync(
            Session.EarliestDefinedMigrationName,
            Session.CancellationToken
        );
    }

    private ImmutableArray<Migration> GetPendingMigrations(IReadOnlyList<Migration> appliedMigrations)
    {
        var pendingMigrations = new MigrationMerger(Session).Merge(
            definedMigrations: Session.Migrations.AsSpan(),
            appliedMigrations
        );

        MigrationReferenceResolver.Resolve(pendingMigrations.AsSpan());

        return pendingMigrations;
    }

    private static MigrationPlan ComputeMigrationPlan(ImmutableArray<Migration> pendingMigrations)
    {
        return new MigrationPlanner(pendingMigrations).CreatePlan();
    }

    private bool ShouldExecute(MigrationPlan plan)
    {
        // true  => valid
        // false => valid, but nothing to do
        // throw => invalid

        if (plan.PendingMigrations.IsEmpty)
        {
            ReportNoPendingMigrations();
            return false; // nothing to do
        }

        var valid = new MigrationValidator(this).Validate(plan);

        ReportPendingMigrations(plan);
        ReportDiagnostics      (plan.PendingMigrations);

        if (!valid) throw OnInvalid();
 
        if (plan.IsEmpty(Session.CurrentPhase))
        {
            ReportEmptyPlan();
            return false; // nothing to do
        }

        if (!Session.AllowContentInCorePhase && plan.IsCoreRequired)
        {
            ReportCoreRequired();
            throw OnInvalid();
        }

        return true;
    }

    private async Task ExecuteAsync(MigrationPlan plan, IMigrationTargetConnection connection)
    {
        ReportApplying();

        if (Session.CurrentPhase is MigrationPhase.Pre)
            await connection.InitializeMigrationSupportAsync(Session.CancellationToken);

        foreach (var (migration, phase) in plan.GetItems(Session.CurrentPhase))
        {
            ReportApplying(migration, phase);

            await connection.ExecuteMigrationContentAsync(
                migration, phase, Session.CancellationToken
            );

            _appliedCount++;
        }
    }

    private void ReportStarting()
    {
        _stopwatch.Restart();

        Console.ReportStarting(this);

        var i = ProcessInfo.Instance;

        Log("PSql.Deploy Migration Log");
        Log("");
        Log($"Migration Phase:    {Session.CurrentPhase}");
        Log($"Target Server:      {Target.ServerDisplayName}");
        Log($"Target Database:    {Target.DatabaseDisplayName}");
        Log($"Start Time:         {DateTime.UtcNow:o}");
        Log($"Machine:            {i.MachineName}");
        Log($"Logical CPUs:       {i.ProcessorCount}");
        Log($"User:               {i.UserName}");
        Log($"Process:            {i.ProcessId} {i.ProcessName} ({i.ProcessArchitecture})");
        Log($"Operating System:   {i.OSDescription} ({i.OSArchitecture})");
        Log($".NET Runtime:       {i.FrameworkDescription}");
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

            //if (!isPlanned)
            //    continue;

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
            if (migration.Diagnostics.Count is 0)
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
        Console.ReportProblem(this, diagnostic.Message);

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
            Target.ServerDisplayName,
            Target.DatabaseDisplayName
        );

        Console.ReportProblem(this, message);

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
        Console.ReportApplying(this, migration.Name, phase);

        Log(string.Concat("[", migration.Name, " ", phase.ToString(), "]"));

        // Server messages appear here
    }

    private void ReportException(Exception exception)
    {
        Console.ReportProblem(this, exception.Message);

        // In the case of a MigrationException, the applicator has already
        // logged helpful diagnostics; log the exception message only, as a
        // summary.  Other exceptions are truly unexpected; log full exception
        // details.
        Log("");
        Log(exception is MigrationException ? exception.Message : exception.ToString());
    }

    private void ReportCanceled()
    {
        Log("");
        Log("Migration application was canceled.");
    }

    private void ReportEnded()
    {
        _stopwatch.Stop();

        var elapsed = _stopwatch.Elapsed;

        Console.ReportApplied(this, _appliedCount, elapsed, _disposition);

        // Footer
        Log("");
        Log($"Applied {_appliedCount} migration(s) in {elapsed.TotalSeconds:N3} second(s).");
    }

    private void BeginLog()
    {
        _logWriter = Console.CreateLog(this);
    }

    private async ValueTask CloseLogAsync()
    {
        Assume.NotNull(_logWriter);

        await _logWriter.FlushAsync();
        await _logWriter.DisposeAsync();

        _logWriter = null;
    }

    /// <summary>
    ///   Writes the specified text and a line ending to the per-database log
    ///   file.
    /// </summary>
    /// <param name="text">
    ///   The text to write.
    /// </param>
    public void Log(string text)
    {
        Assume.NotNull(_logWriter);

        _logWriter.WriteLine(text);
    }

    private static MigrationException OnInvalid()
    {
        return new("Migration validation failed.");
    }
}
