/*
    Copyright 2022 Jeffrey Sharp

    Permission to use, copy, modify, and distribute this software for any
    purpose with or without fee is hereby granted, provided that the above
    copyright notice and this permission notice appear in all copies.

    THE SOFTWARE IS PROVIDED "AS IS" AND THE AUTHOR DISCLAIMS ALL WARRANTIES
    WITH REGARD TO THIS SOFTWARE INCLUDING ALL IMPLIED WARRANTIES OF
    MERCHANTABILITY AND FITNESS. IN NO EVENT SHALL THE AUTHOR BE LIABLE FOR
    ANY SPECIAL, DIRECT, INDIRECT, OR CONSEQUENTIAL DAMAGES OR ANY DAMAGES
    WHATSOEVER RESULTING FROM LOSS OF USE, DATA OR PROFITS, WHETHER IN AN
    ACTION OF CONTRACT, NEGLIGENCE OR OTHER TORTIOUS ACTION, ARISING OUT OF
    OR IN CONNECTION WITH THE USE OR PERFORMANCE OF THIS SOFTWARE.
*/

using System.Management.Automation;
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
        _hostFactory = new TaskHostFactory(host);
    }

    internal void WorkerMain(QueueContext context)
    {
        var name = GetHeader(context);
        var host = _hostFactory.Create(name);
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
