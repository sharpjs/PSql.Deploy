// Copyright Subatomix Research Inc.
// SPDX-License-Identifier: MIT

namespace PSql.Deploy.Seeding;

/// <summary>
///   TODO
/// </summary>
public class SeedSession
{
    /// <summary>
    ///   TODO
    /// </summary>
    public SeedSession(SeedSessionOptions options, ISeedConsole console)
    {
        Console = console;
    }

    /// <summary>
    ///   TODO
    /// </summary>
    public ISeedConsole Console { get; }

    /// <summary>
    ///   TODO
    /// </summary>
    public void BeginApplying(TargetSet targetSet)
    {
    }

    /// <summary>
    ///   TODO
    /// </summary>
    public Task CompleteApplyingAsync(CancellationToken cancellationToken)
    {
        Console.ReportProblem(null, "Foo");
        return Task.CompletedTask;
    }

    /// <summary>
    ///   TODO
    /// </summary>
    public void Dispose()
    {
    }
}
