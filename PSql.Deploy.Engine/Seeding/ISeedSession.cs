// Copyright Subatomix Research Inc.
// SPDX-License-Identifier: MIT

namespace PSql.Deploy.Seeding;

/// <summary>
///   Information about a session in which content seeds are applied to target
///   databases.
/// </summary>
/// <remarks>
///   This interface provides a read-only view of session information.  Code
///   that controls operation of the session should use the
///   <see cref="ISeedSessionControl"/> interface instead.
/// </remarks>
public interface ISeedSession
{
    /// <summary>
    ///   Gets whether the session operates in what-if mode.  In this mode, the
    ///   session reports what actions it would perform against a target
    ///   database but does not perform the actions.
    /// </summary>
    bool IsWhatIfMode { get; }

    /// <summary>
    ///   Gets whether the session has encountered an error.
    /// </summary>
    bool HasErrors { get; }

    /// <summary>
    ///   Gets the user interface via which the session reports progress.
    /// </summary>
    ISeedConsole Console { get; }

    /// <summary>
    ///   Gets the discovered seeds.
    /// </summary>
    /// <remarks>
    ///   The default value is <see cref="ImmutableArray{T}.Empty"/>.  Invoke
    ///   <see cref="ISeedSessionControl.DiscoverSeeds"/> to populate this
    ///   property.
    /// </remarks>
    ImmutableArray<Seed> Seeds { get; }
}
