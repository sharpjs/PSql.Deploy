// Copyright Subatomix Research Inc.
// SPDX-License-Identifier: MIT

using System.Management.Automation.Runspaces;
using Microsoft.PowerShell;

namespace PSql.Deploy;

internal static class ScriptExecutor
{
    private const string
        ModuleFileName = "PSql.Deploy.psd1";

    private static string
        TestPath => TestContext.CurrentContext.TestDirectory;

    private static readonly InitialSessionState
        InitialState = CreateInitialSessionState();

    private static readonly PSInvocationSettings
        Settings = new() { ErrorActionPreference = ActionPreference.Stop };

    private static InitialSessionState CreateInitialSessionState()
    {
        var state = InitialSessionState.CreateDefault();

        if (OperatingSystem.IsWindows())
            state.ExecutionPolicy = ExecutionPolicy.RemoteSigned;

        state.ImportPSModule("PSql");
        state.ImportPSModule(Path.Combine(TestPath, ModuleFileName));

        return state;
    }

    internal static (IReadOnlyList<PSObject?>, Exception?) Execute(string script)
    {
        return Execute(null, script);
    }

    internal static (IReadOnlyList<PSObject?>, Exception?) Execute(Action<InitialSessionState>? setup, string script)
    {
        if (script is null)
            throw new ArgumentNullException(nameof(script));

        var state = InitialState;

        if (setup is not null)
        {
            state = state.Clone();
            setup(state);
        }

        var output    = new List<PSObject?>();
        var exception = null as Exception;

        using var shell = PowerShell.Create(state);

        Redirect(shell.Streams, output);

        shell
            .AddCommand("Set-Location").AddParameter("LiteralPath", TestPath)
            .AddScript(script);

        try
        {
            shell.Invoke(input: null, output, Settings);
        }
        catch (Exception e)
        {
            exception = e;
        }

        exception ??= shell.Streams.Error.FirstOrDefault()?.Exception;

        return (output, exception);
    }

    private static void Redirect(PSDataStreams streams, List<PSObject?> output)
    {
        streams.Warning.DataAdding += (_, data) => StoreWarning (data, output);
        streams.Error  .DataAdding += (_, data) => StoreError   (data, output);
    }

    private static void StoreWarning(DataAddingEventArgs data, List<PSObject?> output)
    {
        var written = (WarningRecord) data.ItemAdded;
        var message = new PSWarning(written.Message);
        output.Add(new PSObject(message));
    }

    private static void StoreError(DataAddingEventArgs data, List<PSObject?> output)
    {
        var written = (ErrorRecord) data.ItemAdded;
        var message = new PSError(written.Exception.Message);
        output.Add(new PSObject(message));
    }
}

internal readonly record struct PSWarning (string Message);
internal readonly record struct PSError   (string Message);
