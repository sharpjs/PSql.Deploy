// Copyright Subatomix Research Inc.
// SPDX-License-Identifier: MIT

namespace PSql.Deploy.Seeds;

/// <summary>
///   A session in which content seeds are applied to target databases.
/// </summary>
public interface ISeedSession : IDeploymentSession
{
    /// <summary>
    ///   Gets the user interface via which the session reports progress.
    /// </summary>
    ISeedConsole Console { get; }

    /// <summary>
    ///   Gets the discovered seeds.
    /// </summary>
    /// <remarks>
    ///   The default value is <see cref="ImmutableArray{T}.Empty"/>.  Invoke
    ///   <see cref="DiscoverSeeds"/> to populate this
    ///   property.
    /// </remarks>
    ImmutableArray<Seed> Seeds { get; }

    /// <summary>
    ///   Discovers seeds in the specified directory path.
    /// </summary>
    /// <param name="path">
    ///   The path of a directory in which to discover seeds.
    /// </param>
    /// <param name="names">
    ///   The names of seeds to discover.
    /// </param>
    /// <exception cref="ArgumentNullException">
    ///   <paramref name="path"/> or <paramref name="names"/> is
    ///   <see langword="null"/>.
    /// </exception>
    void DiscoverSeeds(string path, string[] names);
}
