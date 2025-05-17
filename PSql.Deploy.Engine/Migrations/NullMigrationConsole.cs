// Copyright Subatomix Research Inc.
// SPDX-License-Identifier: MIT

namespace PSql.Deploy.Migrations;

/// <summary>
///   A minimal <see cref="IMigrationConsole"/> that does nothing.
/// </summary>
public class NullMigrationConsole : IMigrationConsole
{
    private NullMigrationConsole() { }

    /// <summary>
    ///   Gets the singleton instance of <see cref="NullMigrationConsole"/>.
    /// </summary>
    public static NullMigrationConsole Instance { get; } = new();

    /// <inheritdoc/>
    public void ReportStarting(IMigrationSession session, Target target)
    {
        // NOP
    }

    /// <inheritdoc/>
    public void ReportApplying(IMigrationSession session, Target target,
        string migrationName, MigrationPhase phase)
    {
        // NOP
    }

    /// <inheritdoc/>
    public void ReportApplied(IMigrationSession session, Target target,
        int count, TimeSpan duration, TargetDisposition disposition)
    {
        // NOP
    }

    /// <inheritdoc/>
    public void ReportProblem(IMigrationSession session, Target? target, string message)
    {
        // NOP
    }

    /// <inheritdoc/>
    public TextWriter CreateLog(IMigrationSession session, Target target)
    {
        return TextWriter.Null;
    }
}
