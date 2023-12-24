// Copyright 2023 Subatomix Research Inc.
// SPDX-License-Identifier: ISC

namespace PSql.Deploy.Seeding;

/// <summary>
///   Information about a session in which content seeds are applied to a set
///   of target databases.
/// </summary>
/// <remarks>
///   This interface is used by lower-level code that consumes the session
///   information.  Higher-level code that controls the operation of the
///   session uses <see cref="ISeedSessionControl"/> instead.
/// </remarks>
public interface ISeedSession
{
    /// <summary>
    ///   Gets the discovered seeds.
    /// </summary>
    ImmutableArray<Seed> Seeds { get; }

    /// <summary>
    ///   Gets whether to operate in what-if mode.  In this mode, code should
    ///   report what actions it would perform against a target database but
    ///   should not perform the actions.
    /// </summary>
    bool IsWhatIfMode { get; }

    /// <summary>
    ///   Gets whether any seed application in the session has encountered an
    ///   error.
    /// </summary>
    bool HasErrors { get; }

    /// <summary>
    ///   Gets the token to monitor for cancellation requests.
    /// </summary>
    CancellationToken CancellationToken { get; }
}
