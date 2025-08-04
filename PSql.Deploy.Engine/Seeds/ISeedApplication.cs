// Copyright Subatomix Research Inc.
// SPDX-License-Identifier: MIT

namespace PSql.Deploy.Seeds;

/// <summary>
///   Information about the application of a content seed to a target database.
/// </summary>
public interface ISeedApplication
{
    /// <summary>
    ///   Gets the seed session.
    /// </summary>
    ISeedSession Session { get; }

    /// <summary>
    ///   Gets an object representing the target database.
    /// </summary>
    Target Target { get; }

    /// <summary>
    ///   Gets the seed being applied to the target database.
    /// </summary>
    LoadedSeed Seed { get; }
}
