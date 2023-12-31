// Copyright 2023 Subatomix Research Inc.
// SPDX-License-Identifier: ISC

namespace PSql.Deploy.Seeding;

/// <inheritdoc/>
internal class SeedConsole : ISeedConsole
{
    /// <summary>
    ///   Initializes a new <see cref="SeedConsole"/> instance that writes
    ///   messages using the specified cmdlet.
    /// </summary>
    /// <param name="cmdlet">
    ///   The cmdlet to use to write messages.
    /// </param>
    /// <exception cref="ArgumentNullException">
    ///   <paramref name="cmdlet"/> is <see langword="null"/>.
    /// </exception>
    public SeedConsole(ICmdlet cmdlet)
    {
        if (cmdlet is null)
            throw new ArgumentNullException(nameof(cmdlet));

        Cmdlet = cmdlet;
    }

    /// <summary>
    ///   Gets the cmdlet to use to write messages.
    /// </summary>
    public ICmdlet Cmdlet { get; }

    /// <inheritdoc/>
    public void ReportStarting()
    {
        Cmdlet.WriteHost("Starting");
    }

    /// <inheritdoc/>
    public void ReportApplying(string moduleName)
    {
        Cmdlet.WriteHost(string.Format(
            "Applying {0}", moduleName
        ));
    }

    /// <inheritdoc/>
    public void ReportApplied(int count, TimeSpan elapsed, TargetDisposition disposition)
    {
        Cmdlet.WriteHost(string.Format(
            "Applied {0} seed(s) in {1:N3} second(s){2}",
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
