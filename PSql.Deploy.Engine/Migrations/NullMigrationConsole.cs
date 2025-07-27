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
    public void ReportStarting(IMigrationApplication info)
    {
        // NOP
    }

    /// <inheritdoc/>
    public void ReportApplying(IMigrationApplication info,
        string migrationName, MigrationPhase phase)
    {
        // NOP
    }

    /// <inheritdoc/>
    public void ReportApplied(IMigrationApplication info,
        int count, TimeSpan duration, TargetDisposition disposition)
    {
        // NOP
    }

    /// <inheritdoc/>
    public void ReportProblem(IMigrationApplication info, string message)
    {
        // NOP
    }

    /// <inheritdoc/>
    public TextWriter CreateLog(IMigrationApplication info)
    {
        return TextWriter.Null;
    }
}
