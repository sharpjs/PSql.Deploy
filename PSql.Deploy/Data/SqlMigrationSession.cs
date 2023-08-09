// Copyright 2023 Subatomix Research Inc.
// SPDX-License-Identifier: ISC

namespace PSql.Deploy.Migrations;

/// <summary>
///   The publicly-exposed surface of a database schema migration session.
/// </summary>
public class SqlMigrationSession
{
    internal SqlMigrationSession(IMigrationSessionControl session)
    {
        if (session is null)
            throw new ArgumentNullException(nameof(session));

        Session = session;
    }

    internal IMigrationSessionControl Session { get; }

    /// <summary>
    ///   Gets or sets the current deployment phase.
    /// </summary>
    MigrationPhase Phase { get; set; }

    /// <summary>
    ///   Gets or sets whether to allow a non-skippable <c>Core</c> phase.
    ///   The default value is <see langword="false"/>.
    /// </summary>
    bool AllowCorePhase { get; set; }

    /// <summary>
    ///   Gets or sets whether to operate in what-if mode.  In this mode, code
    ///   should report what actions it would perform against a target database
    ///   but should not perform the actions.  The default value is
    ///   <see langword="false"/>.
    /// </summary>
    bool IsWhatIfMode { get; set; }
}
