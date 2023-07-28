// Copyright 2023 Subatomix Research Inc.
// SPDX-License-Identifier: ISC

namespace PSql.Deploy.Migrations;

/// <summary>
///   An engine that applies a set of migrations to a set of target databases.
/// </summary>
public interface IMigrationEngine
{
    /// <summary>
    ///   Gets or sets whether the engine allows a non-skippable <c>Core</c>
    ///   phase to exist.  The default is <see langword="false"/>.
    /// </summary>
    bool AllowCorePhase { get; set; }

    /// <summary>
    ///   Gets or sets whether the engine operates in what-if mode.  In this
    ///   mode, the engine reports what actions it would perform against target
    ///   databases but does not perform the actions.
    /// </summary>
    bool IsWhatIfMode { get; set; }

    /// <summary>
    ///   Gets whether migration application to one or more target databases
    ///   failed with an error.
    /// </summary>
    bool HasErrors { get; }

    /// <summary>
    ///   Discovers defined migrations in the specified directory path.
    /// </summary>
    /// <param name="path">
    ///   The path of a directory in which to discover migrations.
    /// </param>
    /// <param name="maxName">
    ///   The maximum (latest) name of migrations to discover, or
    ///   <see langword="null"/> to discover all migrations.
    /// </param>
    void DiscoverMigrations(string path, string? maxName = null);

    /// <summary>
    ///   Specifies the target databases.
    /// </summary>
    /// <param name="contextSets">
    ///   The context sets specifying the target databases.
    /// </param>
    /// <exception cref="ArgumentNullException">
    ///   <paramref name="contextSets"/> is <see langword="null"/>.
    /// </exception>
    void SpecifyTargets(IEnumerable<SqlContextParallelSet> contextSets);

    /// <summary>
    ///   Applies any outstanding migrations for the specified phase to target
    ///   databases asynchronously.
    /// </summary>
    /// <param name="phase">
    ///   The phase in which migrations are being applied.
    /// </param>
    /// <returns>
    ///   A <see cref="Task"/> representing the asynchronous operation.
    /// </returns>
    Task ApplyAsync(MigrationPhase phase);
}
