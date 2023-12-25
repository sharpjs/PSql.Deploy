// Copyright 2023 Subatomix Research Inc.
// SPDX-License-Identifier: ISC

namespace PSql.Deploy.Migrations;

/// <inheritdoc/>
internal class MigrationConsole : IMigrationConsole
{
    /// <summary>
    ///   Initializes a new <see cref="MigrationConsole"/> instance that writes
    ///   messages using the specified cmdlet.
    /// </summary>
    /// <param name="cmdlet">
    ///   The cmdlet to use to write messages.
    /// </param>
    /// <exception cref="ArgumentNullException">
    ///   <paramref name="cmdlet"/> is <see langword="null"/>.
    /// </exception>
    public MigrationConsole(PSCmdlet cmdlet)
    {
        if (cmdlet is null)
            throw new ArgumentNullException(nameof(cmdlet));

        Cmdlet = cmdlet;
    }

    /// <summary>
    ///   Gets the cmdlet to use to write messages.
    /// </summary>
    public PSCmdlet Cmdlet { get; }

    /// <inheritdoc/>
    public void ReportStarting()
    {
        Cmdlet.WriteHost("Starting");
    }

    /// <inheritdoc/>
    public void ReportApplying(string migrationName, MigrationPhase phase)
    {
        Cmdlet.WriteHost(string.Format(
            "Applying {0} ({1})",
            migrationName,
            phase
        ));
    }

    /// <inheritdoc/>
    public void ReportApplied(int count, TimeSpan elapsed, TargetDisposition disposition)
    {
        Cmdlet.WriteHost(string.Format(
            "Applied {0} migration(s) in {1:N3} second(s){2}",
            count,
            elapsed.TotalSeconds,
            disposition.ToMarker()
        ));
    }

    /// <inheritdoc/>
    public void ReportProblem(string message)
    {
        Cmdlet.WriteWarning(message);
    }
}
