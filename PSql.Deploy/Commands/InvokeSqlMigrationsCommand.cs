// Copyright 2024 Subatomix Research Inc.
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
    ///   Latest (maximum) name of migrations to discover.
    /// </summary>
    [Parameter()]
    [ValidateNotNullOrEmpty]
    public string? MaximumMigrationName { get; set; }

    /// <summary>
    ///   <b>-AllowCorePhase:</b>
    ///   Allow a non-skippable <c>Core</c> phase.
    /// </summary>
    [Parameter()]
    public SwitchParameter AllowCorePhase { get; set; }

    private bool IsWhatIf
        => this.IsWhatIf();

    private string CurrentPath
        => this.GetCurrentPath();

    private bool HasMultiplePhases
        => Phase!.Length > 1;

    private static MigrationPhase[] AllPhases
        => new[] { Pre, Core, Post };

    private IMigrationSessionControl?           _session;
    private ICollection<SqlContextParallelSet>? _contextSets;
    private TaskScope?                          _taskScope;
    private int                                 _phaseIndex = -1;

    protected override void BeginProcessingCore()
    {
        ValidatePhase();
        CreateSession();
        AdvanceToNextPhase();

        base.BeginProcessingCore();
    }

    protected override void ProcessContextSet(SqlContextParallelSet contextSet)
    {
        AssertInitialized();

        // Cache context set for later phases
        _contextSets.Add(contextSet);

        base.ProcessContextSet(contextSet);
    }

    protected override Task ProcessWorkAsync(SqlContextWork work)
    {
        AssertInitialized();

        return _session.ApplyAsync(work, this);
    }

    protected override void EndProcessingCore()
    {
        AssertInitialized();

        // Waits for all tasks in progress
        base.EndProcessingCore();

        while (AdvanceToNextPhase())
            ProcessSubsequentPhase();
    }

    [MemberNotNull(nameof(Phase), nameof(_contextSets))]
    private void ValidatePhase()
    {
        if (Phase is null or [])
        {
            Phase = AllPhases;
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

        _contextSets = Phase.Length > 1
            ? new List<SqlContextParallelSet>()
            : EmptyCollection<SqlContextParallelSet>.Instance;
    }

    [MemberNotNull(nameof(_session))]
    private void CreateSession()
    {
        var path    = CurrentPath;
        var console = new MigrationConsole(this);

        _session = MigrationSessionFactory.Create(console, path, CancellationToken);

        _session.AllowCorePhase = AllowCorePhase;
        _session.IsWhatIfMode   = IsWhatIf;

        _session.DiscoverMigrations(Path ?? path, MaximumMigrationName);
    }

    private bool AdvanceToNextPhase()
    {
        AssertInitialized();

        if (_phaseIndex >= Phase.Length)
            return false;

        var phase = Phase[++_phaseIndex];

        _taskScope?.Dispose();
        _taskScope = TaskScope.Begin(phase.ToString());

        _session.Phase = phase;

        return true;
    }

    // Re-runs cmdlet logic in new phase with cached context sets
    private void ProcessSubsequentPhase()
    {
        AssertInitialized();

        base.BeginProcessingCore();

        foreach (var contextSet in _contextSets)
            base.ProcessContextSet(contextSet);

        base.EndProcessingCore();
    }

    [Conditional("DEBUG")]
    [MemberNotNull(nameof(Phase), nameof(_session), nameof(_contextSets))]
    private void AssertInitialized()
    {
        Debug.Assert(Phase        is not null);
        Debug.Assert(_session     is not null);
        Debug.Assert(_contextSets is not null);
    }

    [DoesNotReturn]
    private static void ThrowInvalidPhase()
    {
        throw new ValidationMetadataException(
            "-Phase must be a unique, ordered array of valid phases. " +
            "The possible phases are Pre, Core, and Post."
        );
    }

    protected override void Dispose(bool managed)
    {
        if (managed)
            _taskScope?.Dispose();

        base.Dispose(managed);
    }
}
