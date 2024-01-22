// Copyright 2024 Subatomix Research Inc.
// SPDX-License-Identifier: ISC

using System.Collections.Concurrent;

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
    // Internals
    private readonly ConcurrentBag<Exception> _exceptions;
    //private          TaskHostFactory?         _hostFactory;

    // Parameters
    private ScriptBlock?    _scriptBlock;
    private PSModuleInfo[]? _modules;
    private PSVariable[]?   _variables;

    public InvokeForEachSqlContextCommand()
    {
        _exceptions = new();
    }

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

    protected override Task ProcessWorkAsync(SqlContextWork work)
    {
        throw new NotImplementedException();
    }

    #if OLD

    protected override void BeginProcessing()
    {
        base.BeginProcessing();

        _hostFactory = new(Host);
    }

    protected override void ProcessRecord()
    {
        if (CancellationToken.IsCancellationRequested)
            return;

        if (ParameterSetName == ContextParameterSetName)
            ProcessContextSet(SynthesizeParallelSet());
        else
            foreach (var contextSet in Target)
                ProcessContextSet(contextSet);
    }

    private SqlContextParallelSet SynthesizeParallelSet()
    {
        return new()
        {
            Contexts    = (IReadOnlyList<SqlContext>) Context!,
            Parallelism = Parallelism
        };
    }

    private void ProcessContextSet(SqlContextParallelSet contextSet)
    {
        if (contextSet.Contexts.Count == 0)
            return;

        Task ProcessAsync()
            => ProcessContextSetAsync(contextSet);

        Run(ProcessAsync);
    }

    private async Task ProcessContextSetAsync(SqlContextParallelSet contextSet)
    {
        using var limiter = new SemaphoreSlim(
            initialCount: contextSet.Parallelism,
            maxCount:     contextSet.Parallelism
        );

        Task ProcessAsync(SqlContext context)
            => ProcessContextAsync(context, limiter);

        await Task.WhenAll(contextSet.Contexts.Select(ProcessAsync));
    }

    private async Task ProcessContextAsync(SqlContext context, SemaphoreSlim limiter)
    {
        // Move to another thread so that caller's context iterator continues
        await Task.Yield();

        // Limit parallelism
        await limiter.WaitAsync(CancellationToken);
        try
        {
            await ProcessContextCoreAsync(context);
        }
        finally
        {
            limiter.Release();
        }
    }

    private async Task ProcessContextCoreAsync(SqlContext context)
    {
        var work = new SqlContextWork(context);

        using var scope = _hostFactory!.BeginScope(work.FullDisplayName);

        try
        {
            await RunScriptAsync(scope.Host, work);
        }
        catch (Exception e)
        {
            StopProcessing();
            HandleException(e, scope.Host, work);
        }
    }

    private async Task RunScriptAsync(PSHost host, SqlContextWork work)
    {
        var state = CreateInitialSessionState(work);

        using var runspace = RunspaceFactory.CreateRunspace(host, state);

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
            .AddParameter("Process", ScriptBlock.Create(ScriptBlock.ToString()))
            .AddParameter("InputObject", work)
            .InvokeAsync(NoInput, SetUpOutput(work));
        // FUTURE: In PS 7.2+, try ScriptBlock.Ast.GetScriptBlock()
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

    private void HandleException(Exception e, PSHost host, SqlContextWork work)
    {
        if (e is AggregateException aggregate)
        {
            foreach (var inner in aggregate.InnerExceptions)
                HandleException(inner, host, work);
        }
        else if (e.InnerException is Exception inner)
        {
            HandleException(inner, host, work);
        }
        else
        {
            _exceptions.Add(e);

            host.UI.WriteErrorLine(GetMostHelpfulMessage(e));

            if (e.Data is { IsReadOnly: false } data)
                data["SqlContextWork"] = work;
        }
    }

    private void ThrowCollectedExceptions()
    {
        if (!_exceptions.TryPeek(out var exception))
            return;

        if (_exceptions.Count == 1)
            ExceptionDispatchInfo.Capture(exception).Throw();

        throw new AggregateException(_exceptions);
    }

    private static string GetMostHelpfulMessage(Exception e)
    {
        if (e is RuntimeException { ErrorRecord: { } error })
        {
            if (error.ErrorDetails?.Message is { Length: > 0 } message0)
                return message0;

            if (error.Exception?.Message is { Length: > 0 } message1)
                return message1;
        }

        return e.Message;
    }
    #endif
}
