// Copyright Subatomix Research Inc.
// SPDX-License-Identifier: MIT

namespace PSql.Deploy;

/// <summary>
///   A governor to limit parallelism within a database deployment operation.
/// </summary>
public class Parallelism : IDisposable
{
    private readonly SemaphoreSlim _targetLimiter;
    private readonly SemaphoreSlim _commandLimiter;

    private bool _isDisposed;

    /// <summary>
    ///   Initializes a new <see cref="Parallelism"/> instance with the
    ///   specified limits.
    /// </summary>
    /// <param name="maxParallelTargets">
    ///   The maximum number of target databases against which a deployment
    ///   operation should run in parallel.  Must be positive.
    /// </param>
    /// <param name="maxParallelCommands">
    ///   The maximum number of commands that a deployment operation should
    ///   execute in parallel across all target databases.  Must be positive.
    /// </param>
    /// <param name="maxCommandsPerTarget">
    ///   The maximum number of commands that a deployment operation should
    ///   execute in parallel against any one target database.  Must be
    ///   positive.
    /// </param>
    /// <exception cref="ArgumentOutOfRangeException">
    ///   <paramref name="maxParallelTargets"/>,
    ///   <paramref name="maxParallelCommands"/>, and/or
    ///   <paramref name="maxCommandsPerTarget"/> is zero or negative.
    /// </exception>
    public Parallelism(int maxParallelTargets, int maxParallelCommands, int maxCommandsPerTarget)
    {
        if (maxParallelTargets <= 0)
            throw new ArgumentOutOfRangeException(nameof(maxParallelTargets));
        if (maxParallelCommands <= 0)
            throw new ArgumentOutOfRangeException(nameof(maxParallelCommands));
        if (maxCommandsPerTarget <= 0)
            throw new ArgumentOutOfRangeException(nameof(maxCommandsPerTarget));

        MaxParallelTargets   = maxParallelTargets;
        MaxParallelCommands  = maxParallelCommands;
        MaxCommandsPerTarget = maxCommandsPerTarget;

        _targetLimiter  = CreateSemaphore(maxParallelTargets);
        _commandLimiter = CreateSemaphore(maxParallelCommands);
    }

    /// <summary>
    ///   Gets the maximum number of target databases against which a
    ///   deployment operation should run in parallel.
    /// </summary>
    public int MaxParallelTargets { get; }

    /// <summary>
    ///   Gets the maximum number of commands that a deployment operation
    ///   should execute in parallel across all target databases.
    /// </summary>
    public int MaxParallelCommands { get; }

    /// <summary>
    ///   Gets the maximum number of commands that a deployment operation
    ///   should execute in parallel against any one target database.
    /// </summary>
    public int MaxCommandsPerTarget { get; }

    /// <summary>
    ///   Establishes a scope to perform a database deployment operation
    ///   against one target database, occupying one unit of target
    ///   parallelism.
    /// </summary>
    /// <param name="cancellation">
    ///   The token to monitor for cancellation requests.
    /// </param>
    /// <returns>
    ///   An object that releases the unit of target parallelism on disposal.
    /// </returns>
    /// <exception cref="ObjectDisposedException">
    ///   The object has been disposed.
    /// </exception>
    public async Task<IDisposable> UseTargetScopeAsync(CancellationToken cancellation = default)
    {
        await _targetLimiter.WaitAsync(cancellation).ConfigureAwait(false);
        return new Releaser(_targetLimiter);
    }

    /// <summary>
    ///   Establishes a scope to perform one linear sequence of commands
    ///   against a target database, occupying one unit of command parallelism.
    /// </summary>
    /// <param name="cancellation">
    ///   The token to monitor for cancellation requests.
    /// </param>
    /// <returns>
    ///   An object that releases the unit of command parallelism on disposal.
    /// </returns>
    /// <exception cref="ObjectDisposedException">
    ///   The object has been disposed.
    /// </exception>
    public async Task<IDisposable> UseCommandScopeAsync(CancellationToken cancellation = default)
    {
        await _commandLimiter.WaitAsync(cancellation).ConfigureAwait(false);
        return new Releaser(_commandLimiter);
    }

    private static SemaphoreSlim CreateSemaphore(int limit)
    {
        return new SemaphoreSlim(initialCount: limit, maxCount: limit);
    }

    /// <summary>
    ///   Releases the resources used by the object.
    /// </summary>
    public void Dispose()
    {
        Dispose(managed: true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    ///   Releases the resources used by the object.
    /// </summary>
    /// <param name="managed">
    ///   <see langword="true"/> to dispose managed and unmanaged resources;
    ///   <see langword="false"/> to dispose unmanaged resources only.
    /// </param>
    protected virtual void Dispose(bool managed)
    {
        if (_isDisposed)
            return;

        if (managed)
        {
            _targetLimiter .Dispose();
            _commandLimiter.Dispose();
        }

        _isDisposed = true;
    }

    private sealed class Releaser : IDisposable
    {
        private SemaphoreSlim? _semaphore;

        public Releaser(SemaphoreSlim semaphore)
        {
            _semaphore = semaphore;
        }

        public void Dispose()
        {
            Interlocked.Exchange(ref _semaphore, null)?.Release();
        }
    }
}
