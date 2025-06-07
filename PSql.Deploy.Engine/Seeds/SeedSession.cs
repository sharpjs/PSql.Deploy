// Copyright Subatomix Research Inc.
// SPDX-License-Identifier: MIT

namespace PSql.Deploy.Seeds;

/// <summary>
///   A deployment session in which one or more content seeds are applied to
///   target databases.
/// </summary>
public class SeedSession : DeploymentSession, ISeedSessionInternal
{
    /// <summary>
    ///   Initializes a new <see cref="SeedSession"/> instance.
    /// </summary>
    /// <param name="options">
    ///   Options for the session.
    /// </param>
    /// <param name="console">
    ///   The user interface via which to report progress.
    /// </param>
    /// <param name="maxErrorCount">
    ///   The maximum count of exceptions that the session should tolerate
    ///   before cancelling ongoing operations.  Must be a positive number.
    /// </param>
    /// <exception cref="ArgumentNullException">
    ///   <paramref name="console"/> is <see langword="null"/>.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    ///   <paramref name="maxErrorCount"/> is <c>0</c> or negative.
    /// </exception>
    public SeedSession(SeedSessionOptions options, ISeedConsole console, int maxErrorCount = 1)
        : base(maxErrorCount)
    {
        if (console is null)
            throw new ArgumentNullException(nameof(console));

        Options = options;
        Console = console;
    }

    /// <summary>
    ///   Gets thie options for the session.
    /// </summary>
    public SeedSessionOptions Options { get; }

    /// <summary>
    ///   Gets the user interface via which to report progress.
    /// </summary>
    public ISeedConsole Console { get; }

    /// <inheritdoc/>
    public override bool IsWhatIfMode
        => (Options & SeedSessionOptions.IsWhatIfMode) is not 0;

    /// <inheritdoc/>
    public ImmutableArray<Seed> Seeds { get; private set; }

    /// <inheritdoc/>
    public void DiscoverSeeds(string path, string[] names)
    {
    }

    /// <inheritdoc/>
    protected override Task ApplyCoreAsync(Target target, int maxParallelism)
    {
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    protected override Exception Transform(Exception exception)
    {
        return exception as SeedException
            ?? new SeedException(message: null, exception);
    }
}
