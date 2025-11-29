// Copyright Subatomix Research Inc.
// SPDX-License-Identifier: MIT

using System.Management.Automation.Runspaces;
using Microsoft.PowerShell;

namespace PSql.Deploy;

internal static class ScriptExecutor
{
    private const string
        ModuleFileName      = "PSql.Deploy.psd1",
        TestingVariableName = "PSQL_DEPLOY_TESTING";

    private static string
        TestPath => TestContext.CurrentContext.TestDirectory;

    private static readonly InitialSessionState
        InitialState = CreateInitialSessionState();

    private static readonly SessionStateVariableEntry
        TestingVariable = new(TestingVariableName, "1", null);

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

    public static void WithTestingVariable(InitialSessionState state)
    {
        state.EnvironmentVariables.Add(TestingVariable);
    }

    internal static (IReadOnlyList<PSObject?>, Exception?) Execute(string script)
    {
        return Execute(null, script);
    }

    internal static (IReadOnlyList<PSObject?>, Exception?) Execute(Action<InitialSessionState>? setup, string script)
    {
        ArgumentNullException.ThrowIfNull(script);

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
        streams.Debug      .DataAdding += (_, data) => StoreDebug       (data, output);
        streams.Verbose    .DataAdding += (_, data) => StoreVerbose     (data, output);
        streams.Information.DataAdding += (_, data) => StoreInformation (data, output);
        streams.Warning    .DataAdding += (_, data) => StoreWarning     (data, output);
        streams.Error      .DataAdding += (_, data) => StoreError       (data, output);
        streams.Progress   .DataAdding += (_, data) => StoreProgress    (data, output);
    }

    private static void StoreDebug(DataAddingEventArgs data, List<PSObject?> output)
    {
        var written = (DebugRecord) data.ItemAdded;
        var message = new PSDebug(written.Message);
        output.Add(new PSObject(message));
    }

    private static void StoreVerbose(DataAddingEventArgs data, List<PSObject?> output)
    {
        var written = (VerboseRecord) data.ItemAdded;
        var message = new PSVerbose(written.Message);
        output.Add(new PSObject(message));
    }

    private static void StoreInformation(DataAddingEventArgs data, List<PSObject?> output)
    {
        var written = (InformationRecord) data.ItemAdded;
        var message = new PSInformation(written.ToString());
        output.Add(new PSObject(message));
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

    private static void StoreProgress(DataAddingEventArgs data, List<PSObject?> output)
    {
        var written = (ProgressRecord) data.ItemAdded;
        var message = new PSProgress(written.ToString());
        output.Add(new PSObject(message));
    }
}

internal readonly record struct PSDebug       (string Message);
internal readonly record struct PSVerbose     (string Message);
internal readonly record struct PSInformation (string Message);
internal readonly record struct PSWarning     (string Message);
internal readonly record struct PSError       (string Message);
internal readonly record struct PSProgress    (string Message);
