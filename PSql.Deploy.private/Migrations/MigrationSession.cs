// Copyright Subatomix Research Inc.
// SPDX-License-Identifier: MIT

namespace PSql.Deploy.Migrations;

/// <inheritdoc cref="M.MigrationSession"/>
public class MigrationSession : IDisposable
{
    private readonly M.MigrationSession _inner;

    /// <summary>
    ///   Initializes a new <see cref="MigrationSession"/> instance with the
    ///   specified options and console.
    /// </summary>
    /// <param name="options">
    ///   The options for the session.  If the options specify no phases, the
    ///   session enables <see cref="MigrationSessionOptions.AllPhases"/>.
    /// </param>
    /// <param name="cmdlet">
    ///   The cmdlet through which to report progress of the session.
    /// </param>
    /// <param name="logPath">
    ///   The path of the directory in which to save per-target log files.
    /// </param>
    /// <exception cref="ArgumentNullException">
    ///   <paramref name="cmdlet"/> and/or
    ///   <paramref name="logPath"/> is <see langword="null"/>.
    /// </exception>
    public MigrationSession(MigrationSessionOptions options, ICmdlet cmdlet, string logPath)
    {
        _inner = new(
            (M.MigrationSessionOptions) options,
            new CmdletMigrationConsole(cmdlet, logPath)
        );
    }

    /// <inheritdoc cref="M.MigrationSession.BeginApplying(D.TargetSet)"/>
    public void BeginApplying(TargetSet targetSet)
        => _inner.BeginApplying(targetSet.InnerTargetSet);

    /// <inheritdoc cref="M.MigrationSession.BeginApplying(D.Target)"/>
    public void BeginApplying(Target target)
        => _inner.BeginApplying(target.InnerTarget);

    /// <inheritdoc cref="M.MigrationSession.CompleteApplyingAsync"/>
    public Task CompleteApplyingAsync(CancellationToken cancellation = default)
        => _inner.CompleteApplyingAsync(cancellation);

    /// <inheritdoc cref="M.MigrationSession.Dispose"/>
    public void Dispose()
        => _inner.Dispose();
}
