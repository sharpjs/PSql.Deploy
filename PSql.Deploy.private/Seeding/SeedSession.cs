// Copyright Subatomix Research Inc.
// SPDX-License-Identifier: MIT

namespace PSql.Deploy.Seeding;

internal class SeedSession
{
    internal void BeginApplying(TargetSet targetSet)
    {
    }

    internal Task CompleteApplyingAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    internal void Dispose()
    {
    }
}
