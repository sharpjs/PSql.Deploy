#if ADJUST_FOR_TASKHOST_2
// Copyright 2023 Subatomix Research Inc.
// SPDX-License-Identifier: ISC

using System.Management.Automation.Host;

namespace PSql.Deploy.Seeding;

/// <summary>
///   A factory for <see cref="SeedPlan"/> objects that share a common worker
///   script and PowerShell host.
/// </summary>
public class SeedPlanFactory
{
    private readonly SeedWorker _worker;

    /// <summary>
    ///   Initializes a new <see cref="SeedPlanFactory"/> instance using the
    ///   specified worker script and PowerShell host.
    /// </summary>
    /// <param name="script">
    ///   A PowerShell script that contains the worker implementation to use
    ///   for all <see cref="SeedPlan"/> objects the factory creates.  See the
    ///   remarks section for implementation guidance.
    /// </param>
    /// <param name="host">
    ///   The PowerShell host to which to redirect worker output.
    /// </param>
    /// <exception cref="ArgumentNullException">
    ///   <paramref name="script"/>, <paramref name="host"/>, and/or the host's
    ///   <see cref="PSHost.UI"/> property is <see langword="null"/>.
    /// </exception>
    /// <remarks>
    ///   The seed runner provides a <see cref="SeedContext"/> object in the
    ///   <c>$Seed</c> variable in each <paramref name="script"/> invocation.
    ///   The script should contain a loop that invokes the
    ///   <see cref="SeedContext.GetNextModule"/> method, which returns a
    ///   <see cref="SeedModule"/> object or <see langword="null"/>.  While the
    ///   result is not <see langword="null"/>, the script should enumerate the
    ///   <see cref="SeedModule.Batches"/> property and execute each SQL batch
    ///   it contains.
    /// </remarks>
    public SeedPlanFactory(string script, PSHost host)
    {
        if (script is null)
            throw new ArgumentNullException(nameof(script));
        // host null-check performed by TaskHostFactory constructor

        _worker = new SeedWorker(script, host);
    }

    /// <summary>
    ///   Creates a new <see cref="SeedPlan"/> instance.
    /// </summary>
    public SeedPlan Create()
    {
        return new SeedPlan(_worker);
    }
}
#endif
