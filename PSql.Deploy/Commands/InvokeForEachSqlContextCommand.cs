#if CONVERTED
// Copyright Subatomix Research Inc.
// SPDX-License-Identifier: MIT

using System.Collections;
using System.Management.Automation.Runspaces;

namespace PSql.Deploy.Commands;

/// <summary>
///   The <c>Invoke-ForEachSqlContext</c> cmdlet.
/// </summary>
/// <remarks>
///   Invokes a <see cref="System.Management.Automation.ScriptBlock"/> for each
///   <see cref="SqlContext"/> in one or more sets.
/// </remarks>
[Cmdlet(VerbsLifecycle.Invoke, "ForEachSqlContext",
    DefaultParameterSetName = ContextSetParameterSetName,
    ConfirmImpact           = ConfirmImpact.Low,
    RemotingCapability      = RemotingCapability.None
)]
public class InvokeForEachSqlContextCommand : PerSqlContextCommand
{
    // Parameters
    private ScriptBlock?    _scriptBlock;
    private PSModuleInfo[]? _modules;
    private PSVariable[]?   _variables;

    /// <summary>
    ///   <b>-ScriptBlock:</b>
    ///   Script block to execute for each <see cref="SqlContext"/>.
    /// </summary>
    [Parameter(Mandatory = true, Position = 0)]
    [ValidateNotNull]
    public ScriptBlock ScriptBlock
    {
        get => _scriptBlock ??= ScriptBlock.Create("");
        set => _scriptBlock   = value;
    }

    /// <summary>
    ///   <b>-Module:</b>
    ///   Modules to import before invoking <see cref="ScriptBlock"/>.
    /// </summary>
    [Parameter()]
    [ValidateNotNull]
    [AllowEmptyCollection]
    public PSModuleInfo[] Module
    {
        get => _modules ??= Array.Empty<PSModuleInfo>();
        set => _modules   = value.Sanitize();
    }

    /// <summary>
    ///   <b>-Variable:</b>
    ///   Variables to predefine before invoking <see cref="ScriptBlock"/>.
    /// </summary>
    [Parameter()]
    [ValidateNotNull]
    [AllowEmptyCollection]
    public PSVariable[] Variable
    {
        get => _variables ??= Array.Empty<PSVariable>();
        set => _variables   = value.Sanitize();
    }

    protected override async Task ProcessWorkAsync(SqlContextWork work)
    {
        var state = CreateInitialSessionState(work);

        using var runspace = RunspaceFactory.CreateRunspace(/*host, */state);

        runspace.Name          = work.FullDisplayName;
        runspace.ThreadOptions = PSThreadOptions.UseCurrentThread;
        runspace.Open();

        await RunScriptAsync(runspace, work);

        runspace.Close();
    }

    private InitialSessionState CreateInitialSessionState(SqlContextWork work)
    {
        var state = InitialSessionState.CreateDefault();

        foreach (var module in Module)
            state.ImportPSModule(new[] { module.Path });

        foreach (var variable in Variable)
            state.Variables.Add(new SessionStateVariableEntry(
                variable.Name,
                variable.Value,
                variable.Description,
                variable.Options,
                variable.Attributes
            ));

        state.Variables.Add(new SessionStateVariableEntry(
            "CancellationToken", CancellationToken, null
        ));

        state.Variables.Add(new SessionStateVariableEntry(
            "ErrorActionPreference", "Stop", null
        ));

        return state;
    }

    private async Task RunScriptAsync(Runspace runspace, SqlContextWork work)
    {
        const PSDataCollection<PSObject>? NoInput = null;

        using var shell = PowerShell.Create();

        shell.Runspace = runspace;

        await shell
            .AddCommand("ForEach-Object")
            .AddParameter("Process", ScriptBlock.Clone())
            .AddParameter("InputObject", work.Context)
            .InvokeAsync(NoInput, SetUpOutput(work));
    }

    private PSDataCollection<PSObject> SetUpOutput(SqlContextWork work)
    {
        var output = new PSDataCollection<PSObject>();
        output.DataAdding += (_, e) => HandleOutput(work.FullDisplayName, e.ItemAdded);
        return output;
    }

    private void HandleOutput(string source, object? obj)
    {
        var item = new DictionaryEntry(source, obj);

        WriteOutput(item);
    }

    // This method makes WriteObject virtual to accommodate testing.
    protected virtual void WriteOutput(object? output)
    {
        WriteObject(output);
    }
}
#endif
