// Copyright Subatomix Research Inc.
// SPDX-License-Identifier: MIT

namespace PSql.Deploy.Migrations;

/// <summary>
///   Control surface for a session in which schema migrations are applied to
///   target databases.
/// </summary>
/// <remarks>
///   This interface is intended for code that controls the operation of the
///   session.  Code that only consumes session information should use the
///   read-only <see cref="IMigrationSession"/> interface instead.
/// </remarks>
public interface IMigrationSessionControl : IMigrationSession
{
    /// <summary>
    ///   Discovers migrations in the specified directory path.
    /// </summary>
    /// <param name="path">
    ///   The path of a directory in which to discover migrations.
    /// </param>
    /// <param name="latestName">
    ///   The latest (maximum) name of migrations to discover, or
    ///   <see langword="null"/> to discover all migrations.
    /// </param>
    /// <remarks>
    ///   <para>
    ///     This method populates the
    ///     <see cref="IMigrationSession.Migrations"/> and
    ///     <see cref="IMigrationSession.EarliestDefinedMigrationName"/>
    ///     properties.
    ///   </para>
    ///   <para>
    ///     Invoke this method only when migration application is not
    ///     in progress.  Otherwise, this method throws
    ///     <see cref="InvalidOperationException"/>.
    ///   </para>
    /// </remarks>
    /// <exception cref="ArgumentNullException">
    ///   <paramref name="path"/> is <see langword="null"/>.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    ///   The method was invoked while migration application was in progress.
    /// </exception>
    /// <exception cref="IOException">
    ///   An input/output error occured while discovering migrations.
    /// </exception>
    void DiscoverMigrations(string path, string? latestName = null);

    /// <summary>
    ///   Begins applying migrations to the specified target database.
    /// </summary>
    /// <param name="target">
    ///   An object representing a target database.
    /// </param>
    /// <remarks>
    ///   This method returns immediately.  Migration application to the
    ///   <paramref name="target"/> occurs asynchronously and in parallel with
    ///   other targets.
    /// </remarks>
    /// <exception cref="ArgumentNullException">
    ///   <paramref name="target"/> is <see langword="null"/>.
    /// </exception>
    void BeginApplying(Target target);

    /// <summary>
    ///   Begins applying migrations to the specified set of target databases
    ///   with controlled parallelism.
    /// </summary>
    /// <param name="targets">
    ///   An object representing a set of target databases with controlled
    ///   parallelism.
    /// </param>
    /// <remarks>
    ///   This method returns immediately.  Migration application to the
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
    ///   Completes applying migrations asynchronously.
    /// </summary>
    /// <param name="cancellation">
    ///   The token to monitor for cancellation requests.
    /// </param>
    /// <returns>
    ///   A <see cref="Task"/> representing the asynchronous operation.
    /// </returns>
    Task CompleteApplyingAsync(CancellationToken cancellation = default);
}
