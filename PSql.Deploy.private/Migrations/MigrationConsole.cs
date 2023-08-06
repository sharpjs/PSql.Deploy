// Copyright 2023 Subatomix Research Inc.
// SPDX-License-Identifier: ISC

namespace PSql.Deploy.Migrations;

/// <inheritdoc/>
internal class MigrationConsole : IMigrationConsole
{
    /// <summary>
    ///   Initializes a new <see cref="MigrationConsole"/> instance using the
    ///   specified underlying console implementation.
    /// </summary>
    /// <param name="console">
    ///   The underlying console implementation.
    /// </param>
    /// <exception cref="ArgumentNullException">
    ///   <paramref name="console"/> is <see langword="null"/>.
    /// </exception>
    public MigrationConsole(IConsole console)
    {
        if (console is null)
            throw new ArgumentNullException(nameof(console));

        Console = console;
    }

    /// <summary>
    ///   Gets the underlying console implementation.
    /// </summary>
    public IConsole Console { get; }

    /// <inheritdoc/>
    public void ReportStarting()
    {
        Console.WriteHost("Starting");
    }

    /// <inheritdoc/>
    public void ReportApplying(string migrationName, MigrationPhase phase)
    {
        Console.WriteHost(string.Format(
            "Applying {0} ({1})",
            migrationName,
            phase
        ));
    }

    /// <inheritdoc/>
    public void ReportApplied(int count, TimeSpan elapsed, MigrationTargetDisposition disposition)
    {
        Console.WriteHost(string.Format(
            "Applied {0} migration(s) in {1:N3} second(s){2}",
            count,
            elapsed.TotalSeconds,
            disposition.ToMarker()
        ));
    }

    /// <inheritdoc/>
    public void ReportProblem(string message)
    {
        Console.WriteWarning(message);
    }
}
