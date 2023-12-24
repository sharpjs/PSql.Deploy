// Copyright 2023 Subatomix Research Inc.
// SPDX-License-Identifier: ISC

namespace PSql.Deploy.Seeding;

/// <summary>
///   Control surface for a session in which content seeds are applied to a set
///   of target databases.
/// </summary>
/// <remarks>
///   This interface is used by higher-level code that controls the operation
///   of the session.  Lower-level code that consumes the session information
///   uses <see cref="ISeedSession"/> instead.
/// </remarks>
public interface ISeedSessionControl : ISeedSession
{
    /// <summary>
    ///   Gets or sets whether to operate in what-if mode.  In this mode, code
    ///   should report what actions it would perform against a target database
    ///   but should not perform the actions.  The default value is
    ///   <see langword="false"/>.
    /// </summary>
    new bool IsWhatIfMode { get; set; }

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
    ///   Applies discovered seeds to the specified target database
    ///   asynchronously.
    /// </summary>
    /// <param name="target">
    ///   An object specifying the target database.
    /// </param>
    /// <param name="cmdlet">
    ///   The cmdlet through which to report progress.
    /// </param>
    /// <returns>
    ///   A <see cref="Task"/> representing the asynchronous operation.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    ///   <paramref name="target"/> or <paramref name="target"/> is
    ///   <see langword="null"/>.
    /// </exception>
    Task ApplyAsync(SqlContextWork target, PSCmdlet cmdlet);
}
