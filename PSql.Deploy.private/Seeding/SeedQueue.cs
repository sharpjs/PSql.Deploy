// Copyright 2023 Subatomix Research Inc.
// SPDX-License-Identifier: ISC

using DependencyQueue;

namespace PSql.Deploy.Seeding;

using Queue  = DependencyQueue      <IEnumerable<string>>;
using Module = DependencyQueueEntry <IEnumerable<string>>;

// Helper to expose a seed queue to tests without requiring a reference to DependencyQueue
internal static class SeedQueue
{
    internal static object Create()
        => new DependencyQueue<IEnumerable<string>>(StringComparer.OrdinalIgnoreCase);

    internal static IReadOnlyList<object> Validate(object queue)
        => Upcast(queue).Validate();

    internal static IReadOnlyList<(string Name, IEnumerable<string> Value)> Enumerate(object queue)
        => Enumerate(Upcast(queue)).Select(m => (m.Name, m.Value)).ToList();

    private static IEnumerable<Module> Enumerate(Queue queue)
    {
        for (;;)
        {
            var module = queue.TryDequeue();
            if (module is null)
                yield break;

            yield return module;
            queue.Complete(module);
        }
    }

    private static Queue Upcast(object obj) => (Queue) obj;
}
