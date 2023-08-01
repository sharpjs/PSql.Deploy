// Copyright 2023 Subatomix Research Inc.
// SPDX-License-Identifier: ISC

using System.Management.Automation.Host;
using DependencyQueue;
using Subatomix.PowerShell.TaskHost;

namespace PSql.Deploy.Seeding;

using QueueContext = DependencyQueueContext<IEnumerable<string>, Dictionary<string, object?>>;

internal class SeedWorker
{
    private readonly string          _script;
    private readonly TaskHostFactory _hostFactory;

    internal SeedWorker(string script, PSHost host)
    {
        _script      = script;
        _hostFactory = new TaskHostFactory(host, withElapsed: true);
    }

    internal void WorkerMain(QueueContext context)
    {
        var name = GetHeader(context);
        using var host = _hostFactory.Create(name);
        try
        {
            host.UI.WriteLine("Starting");
            Invoke(_script, context, host);
        }
        catch (Exception e)
        {
            host.UI.WriteErrorLine(e.Message);
            throw;
        }
        finally
        {
            host.UI.WriteLine("Ended");
            context.SetEnding();
        }
    }

    private static string GetHeader(QueueContext context)
    {
        var prefix
            =  context.Data.TryGetValue("Name", out var v)
            && v is string s
            && !string.IsNullOrEmpty(s)
            ?  s
            :  "Seed";

        return Invariant($"{prefix}:{context.WorkerId}");
    }

    private static void Invoke(string script, QueueContext context, TaskHost host)
    {
        const ScopedItemOptions Options
            = ScopedItemOptions.Constant
            | ScopedItemOptions.AllScope;

        using var shell = PowerShell.Create();

        shell.Runspace.SessionStateProxy.PSVariable.Set(new(
            "Seed", new SeedContext(context, host), Options
        ));

        var settings = new PSInvocationSettings
        {
            Host                  = host,
            ErrorActionPreference = ActionPreference.Stop
        };

        shell.AddScript(script).Invoke(null, settings);
    }
}
