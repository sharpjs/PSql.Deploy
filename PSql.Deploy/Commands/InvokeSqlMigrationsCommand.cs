// Copyright Subatomix Research Inc.
// SPDX-License-Identifier: ISC

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using PSql.Deploy.Migrations;
using PSql.Deploy.Utilities;
using Subatomix.PowerShell.TaskHost;

namespace PSql.Deploy.Commands;

using static MigrationPhase;

/// <summary>
///   The <c>Invoke-SqlMigrations</c> cmdlet.
/// </summary>
/// <remarks>
///   Invokes database schema migrations against sets of target databases.
/// </remarks>
[Cmdlet(
    VerbsLifecycle.Invoke, "SqlMigrations",
    DefaultParameterSetName = ContextParameterSetName,
    SupportsShouldProcess   = true, // -Confirm and -WhatIf
    ConfirmImpact           = ConfirmImpact.High
)]
public class InvokeSqlMigrationsCommand : PerSqlContextCommand
{
    /// <summary>
    ///   <b>-Path:</b>
    ///   Path to a directory containing migrations.
    /// </summary>
    [Parameter()]
    [Alias("PSPath", "SourcePath")]
    [ValidateNotNullOrEmpty]
    public string? Path { get; set; }

    /// <summary>
    ///   <b>-Phase:</b>
    ///   Deployment phases in which to run migrations.
    /// </summary>
    [Parameter()]
    [ValidateSet(nameof(Pre), nameof(Core), nameof(Post))]
    public MigrationPhase[]? Phase { get; set; }

    /// <summary>
    ///   <b>-MaximumMigrationName:</b>
    ///   Maximum name of migrations to invoke.
    /// </summary>
    [Parameter()]
    [ValidateNotNullOrEmpty]
    public string? MaximumMigrationName { get; set; }

    /// <summary>
    ///   <b>-AllowCorePhase:</b>
    ///   Allow migration content in the <c>Core</c> phase.
    /// </summary>
    [Parameter()]
    public SwitchParameter AllowCorePhase { get; set; }

    private IMigrationSessionControl?           _session;
    private ICollection<SqlContextParallelSet>? _contextSets;
    private TaskScope?                          _taskScope;
    private int                                 _phaseIndex;

    /// <inheritdoc/>
    protected override void BeginProcessingCore()
    {
        ValidatePhases();
        CreateSession();
        SetPhase();

        base.BeginProcessingCore();
    }

    /// <inheritdoc/>
    protected override void ProcessContextSet(SqlContextParallelSet contextSet)
    {
        CacheContextSet(contextSet);

        base.ProcessContextSet(contextSet);
    }

    /// <inheritdoc/>
    protected override Task ProcessWorkAsync(SqlContextWork work)
    {
        AssertInitialized();

        return _session.ApplyAsync(work, this);
    }

    /// <inheritdoc/>
    protected override void EndProcessingCore()
    {
        AssertInitialized();

        // Re-run for later phases
        while (AdvanceToNextPhase())
        {
            WaitForAsyncActions();
            SetPhase();
            ProcessCachedContextSets();
        }

        base.EndProcessingCore();
    }

    [MemberNotNull(nameof(Phase), nameof(_contextSets))]
    private void ValidatePhases()
    {
        if (Phase is null or [])
        {
            Phase = [Pre, Core, Post]; // all
        }
        else
        {
            var next = Pre;

            foreach (var phase in Phase)
            {
                if (phase < next || phase > Post)
                    ThrowInvalidPhase();

                next = phase + 1;
            }
        }

        // ProcessRecord invokes the first phase.  If invoking multiple phases,
        // the cmdlet must cache each context set provided to ProcessRecord.
        // EndProcessing invokes the subsequent phases, enumerating context
        // sets from the cache.
        _contextSets = Phase.Length > 1
            ? new List<SqlContextParallelSet>()
            : EmptyCollection<SqlContextParallelSet>.Instance;
    }

    [MemberNotNull(nameof(_session))]
    private void CreateSession()
    {
        var path    = this.GetCurrentPath();
        var console = new MigrationConsole(this);

        _session = MigrationSessionFactory.Create(console, path, CancellationToken);

        _session.AllowCorePhase = AllowCorePhase;
        _session.IsWhatIfMode   = this.IsWhatIf();

        _session.DiscoverMigrations(Path ?? path, MaximumMigrationName);
    }

    private bool AdvanceToNextPhase()
    {
        AssertInitialized();

        return ++_phaseIndex < Phase.Length;
    }

    [MemberNotNull(nameof(_taskScope))]
    private void SetPhase()
    {
        AssertInitialized();

        var phase = Phase[_phaseIndex];

        _taskScope?.Dispose();
        _taskScope = TaskScope.Begin(phase.ToString());

        _session.Phase = phase;
    }

    // Caches context set for re-running in later phases
    private void CacheContextSet(SqlContextParallelSet contextSet)
    {
        AssertInitialized();

        _contextSets.Add(contextSet);
    }

    // Re-runs cmdlet logic in new phase with cached context sets
    private void ProcessCachedContextSets()
    {
        AssertInitialized();

        foreach (var contextSet in _contextSets)
            base.ProcessContextSet(contextSet);
    }

    [DoesNotReturn]
    private static void ThrowInvalidPhase()
    {
        throw new ValidationMetadataException(
            "-Phase must be a unique, ordered array of valid phases. " +
            "The possible phases are Pre, Core, and Post, in that order."
        );
    }

    /// <inheritdoc/>
    protected override Exception Transform(Exception exception)
    {
        return exception as MigrationException
            ?? new MigrationException(null, exception);
    }

    [Conditional("DEBUG")]
    [MemberNotNull(nameof(Phase), nameof(_session), nameof(_contextSets))]
    private void AssertInitialized()
    {
        Debug.Assert(Phase        is not null);
        Debug.Assert(_session     is not null);
        Debug.Assert(_contextSets is not null);
    }

    /// <inheritdoc/>
    protected override void Dispose(bool managed)
    {
        if (managed)
            _taskScope?.Dispose();

        base.Dispose(managed);
    }
}
