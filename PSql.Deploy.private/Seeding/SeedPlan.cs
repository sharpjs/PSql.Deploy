#if ADJUST_FOR_TASKHOST_2
// Copyright 2023 Subatomix Research Inc.
// SPDX-License-Identifier: ISC

using DependencyQueue;

namespace PSql.Deploy.Seeding;

using Queue       = DependencyQueue<IEnumerable<string>>;
using ContextData = Dictionary<string, object?>;

/// <summary>
///   Builds and runs SQL seeds.
/// </summary>
public class SeedPlan : IDisposable
{
    private readonly Queue      _queue;
    private readonly SeedWorker _worker;
    private BuildState?         _buildState;

    internal SeedPlan(SeedWorker worker)
    {
        _queue      = new(StringComparer.OrdinalIgnoreCase);
        _worker     = worker;
        _buildState = new(_queue);
    }

    /// <summary>
    ///   Discovers seed modules in the specified string and adds them to the
    ///   seed plan.
    /// </summary>
    /// <param name="sql">
    ///   The string in which to discover seed modules.
    /// </param>
    /// <exception cref="ArgumentNullException">
    ///   <paramref name="sql"/> is <see langword="null"/>.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    ///   The seed plan is frozen and cannot be modified.
    /// </exception>
    public void AddModules(string sql)
    {
        RequireBuilding().Parser.Process(sql);
    }

    /// <summary>
    ///   Sets the specified key/value pair in the <c>$Seed.Data</c> dictionary
    ///   visible to the seed worker script.
    /// </summary>
    /// <param name="key"></param>
    /// <param name="value"></param>
    /// <exception cref="ArgumentNullException">
    ///   <paramref name="key"/> is <see langword="null"/>.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    ///   The seed plan is frozen and cannot be modified.
    /// </exception>
    public void AddContextData(string key, object? value)
    {
        RequireBuilding().ContextData[key] = value;
    }

    /// <summary>
    ///   Checks whether the seed plan is valid.
    /// </summary>
    /// <returns>
    ///   If the seed plan is valid, an empty list; otherwise, a list of errors
    ///   that prevent the seed plan from being valid.
    /// </returns>
    public IReadOnlyList<DependencyQueueError> Validate()
    {
        RequireBuilding().Parser.Complete();
        return _queue.Validate();
    }

    /// <summary>
    ///   Runs the seed, optionally using the specified parallelism.
    /// </summary>
    /// <param name="parallelism">
    ///   The maximum number of seed modules to run in parallel, or
    ///   <see langword="null"/> to use the default parallelism,
    ///   <see cref="Environment.ProcessorCount"/>.
    /// </param>
    /// <exception cref="InvalidOperationException">
    ///   This method has been invoked already.
    /// </exception>
    public void Run(int? parallelism = null)
    {
        var contextData = CompleteBuilding().ContextData;

        Console.CancelKeyPress += HandleCancel;
        try
        {
            _queue.Run(_worker.WorkerMain, contextData, parallelism);
        }
        finally
        {
            Console.CancelKeyPress -= HandleCancel;
        }
    }

    private BuildState RequireBuilding()
    {
        const string Message
            = "The seed plan is frozen and cannot be modified.";

        return _buildState
            ?? throw new InvalidOperationException(Message);
    }

    private BuildState CompleteBuilding()
    {
        const string Message
            = "The seed plan has been run already and cannot rerun.";

        return Interlocked.Exchange(ref _buildState, null)
            ?? throw new InvalidOperationException(Message);
    }

    private static void HandleCancel(object sender, ConsoleCancelEventArgs e)
    {
        // TODO: Is this really what is wanted?
        Environment.Exit(1);
    }

    public void Dispose()
    {
        Dispose(managed: true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool managed)
    {
        if (managed)
            _queue.Dispose();
    }

    private class BuildState
    {
        public BuildState(Queue queue)
        {
            Parser      = new(queue);
            ContextData = new(queue.Comparer);
        }

        public SeedModuleParser Parser      { get; }
        public ContextData      ContextData { get; }
    }
}
#endif
