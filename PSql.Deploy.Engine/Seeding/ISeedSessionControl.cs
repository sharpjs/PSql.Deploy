// Copyright Subatomix Research Inc.
// SPDX-License-Identifier: MIT

namespace PSql.Deploy.Seeding;

/// <summary>
///   Control surface for a session in which content seeds are applied to
///   target databases.
/// </summary>
/// <remarks>
///   This interface is intended for code that controls the operation of the
///   session.  Code that only consumes session information should use the
///   read-only <see cref="ISeedSession"/> interface instead.
/// </remarks>
public interface ISeedSessionControl : ISeedSession
{
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

    /// <summary>
    ///   Begins applying seeds to the specified target database.
    /// </summary>
    /// <param name="target">
    ///   An object representing a target database.
    /// </param>
    /// <remarks>
    ///   This method returns immediately.  Seed application to the
    ///   <paramref name="target"/> occurs asynchronously and in parallel with
    ///   other targets.
    /// </remarks>
    /// <exception cref="ArgumentNullException">
    ///   <paramref name="target"/> is <see langword="null"/>.
    /// </exception>
    void BeginApplying(Target target);

    /// <summary>
    ///   Begins applying seeds to the specified set of target databases
    ///   with controlled parallelism.
    /// </summary>
    /// <param name="targets">
    ///   An object representing a set of target databases with controlled
    ///   parallelism.
    /// </param>
    /// <remarks>
    ///   This method returns immediately.  Seed application to the
    ///   <paramref name="targets"/> occurs asynchronously and in parallel with
    ///   other targets.  Maximum parallelism within the specified
    ///   <paramref name="targets"/> is controlled by <see cref="TargetSet"/>
    ///   properties.
    /// </remarks>
    /// <exception cref="ArgumentNullException">
    ///   <paramref name="targets"/> is <see langword="null"/>.
    /// </exception>
    void BeginApplying(TargetSet targets);

    /// <summary>
    ///   Completes applying seeds asynchronously.
    /// </summary>
    /// <param name="cancellation">
    ///   The token to monitor for cancellation requests.
    /// </param>
    /// <returns>
    ///   A <see cref="Task"/> representing the asynchronous operation.
    /// </returns>
    Task CompleteApplyingAsync(CancellationToken cancellation = default);
}
