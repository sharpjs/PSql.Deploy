// Copyright 2024 Subatomix Research Inc.
// SPDX-License-Identifier: ISC

using System.Collections;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using PSql.Deploy.Seeding;

namespace PSql.Deploy.Commands;

/// <summary>
///   The <c>Invoke-SqlSeed</c> cmdlet.
/// </summary>
/// <remarks>
///   Invokes database content seeds against sets of target databases.
/// </remarks>
[Cmdlet(
    VerbsLifecycle.Invoke, "SqlSeed",
    DefaultParameterSetName = ContextParameterSetName,
    SupportsShouldProcess   = true, // -Confirm and -WhatIf
    ConfirmImpact           = ConfirmImpact.High
)]
public class InvokeSqlSeedCommand : PerSqlContextCommand
{
    /// <summary>
    ///   <b>-Seed:</b>
    ///   Names of seeds to apply.
    /// </summary>
    [Parameter]
    [ValidateNotNullOrEmpty]
    public string[] Seed
    {
        get => _seed ??= Array.Empty<string>();
        set => _seed   = value.Sanitize();
    }
    private string[]? _seed;

    /// <summary>
    ///   <b>-Define:</b>
    ///   SQLCMD preprocessor variables to define.
    /// </summary>
    [Parameter]
    [ValidateNotNull]
    [AllowEmptyCollection]
    public Hashtable? Define { get; set; }

    internal SeedSession.Factory
        SeedSessionFactory { get; set; } = SeedSession.DefaultFactory;

    private ISeedSessionControl? _session;

    protected override void BeginProcessingCore()
    {
        var path    = this.GetCurrentPath();
        var console = new SeedConsole(this);

        _session                = SeedSessionFactory.Invoke(console, path, CancellationToken);
        _session.IsWhatIfMode   = this.IsWhatIf();
        _session.MaxParallelism = MaxParallelism; // PerDatabase
        _session.DiscoverSeeds(path, Seed);

        base.BeginProcessingCore();
    }

    protected override Task ProcessWorkAsync(SqlContextWork work)
    {
        AssertInitialized();

        return _session.ApplyAsync(work);
    }

    /// <inheritdoc/>
    protected override Exception Transform(Exception exception)
    {
        return exception as SeedException
            ?? new SeedException(null, exception);
    }

    [Conditional("DEBUG")]
    [MemberNotNull(nameof(_session))]
    private void AssertInitialized()
    {
        Debug.Assert(_session != null);
    }
}
