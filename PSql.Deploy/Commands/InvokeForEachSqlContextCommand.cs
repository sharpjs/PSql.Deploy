// Copyright 2023 Subatomix Research Inc.
// SPDX-License-Identifier: ISC

using System.Collections;
using System.Collections.Concurrent;
using System.Management.Automation.Host;
using System.Management.Automation.Runspaces;
using System.Runtime.ExceptionServices;
using PSql.Deploy.Utilities;

namespace PSql.Deploy.Commands;

/// <summary>
///   Invokes a <see cref="ScriptBlock"/> for each <see cref="SqlContext"/> in
///   one or more sets.
/// </summary>
[Cmdlet(VerbsLifecycle.Invoke, "ForEachSqlContext",
    DefaultParameterSetName = TargetParameterSetName,
    ConfirmImpact           = ConfirmImpact.Low,
    RemotingCapability      = RemotingCapability.None
)]
public class InvokeForEachSqlContextCommand : Cmdlet, IDisposable
{
    private const string
        TargetParameterSetName  = nameof(Target),
        ContextParameterSetName = nameof(Context);

    // Internals
    private readonly List<Task>               _tasks;
    private readonly MainThreadDispatcher     _dispatcher;
    private readonly ConcurrentBag<Exception> _exceptions;
    private readonly CancellationTokenSource  _cancellation;
    private          TaskHostFactory?         _hostFactory;
    private SynchronizationContext? _previousSynchronizationContext;

    // Parameters
    private ScriptBlock?             _scriptBlock;
    private PSModuleInfo[]?          _modules;
    private PSVariable[]?            _variables;
    private SqlContextParallelSet[]? _targets;
    private SqlContext[]?            _contexts;
    private int                      _parallelism;

    public InvokeForEachSqlContextCommand()
    {
        _tasks        = new();
        _dispatcher   = new();
        _exceptions   = new();
        _cancellation = new();
    }

    // -ScriptBlock
    [Parameter(Mandatory = true, Position = 0)]
    [ValidateNotNull]
    public ScriptBlock ScriptBlock
    {
        get => _scriptBlock ??= ScriptBlock.Create("");
        set => _scriptBlock   = value;
    }

    // -Module
    [Parameter()]
    [ValidateNotNull]
    [AllowEmptyCollection]
    public PSModuleInfo[] Module
    {
        get => _modules ??= Array.Empty<PSModuleInfo>();
        set => _modules   = value.Sanitize();
    }

    // -Variable
    [Parameter()]
    [ValidateNotNull]
    [AllowEmptyCollection]
    public PSVariable[] Variable
    {
        get => _variables ??= Array.Empty<PSVariable>();
        set => _variables   = value.Sanitize();
    }

    // -Target
    [Parameter(Mandatory = true, ValueFromPipeline = true, ParameterSetName = TargetParameterSetName)]
    [ValidateNotNullOrEmpty]
    public SqlContextParallelSet[] Target
    {
        get => _targets ??= Array.Empty<SqlContextParallelSet>();
        set => _targets   = value.Sanitize();
    }

    // -Context
    [Parameter(Mandatory = true, ValueFromPipeline = true, ParameterSetName = ContextParameterSetName)]
    [ValidateNotNullOrEmpty]
    public SqlContext[] Context
    {
        get => _contexts ??= Array.Empty<SqlContext>();
        set => _contexts   = value.Sanitize();
    }

    // -Parallelism
    [Parameter(ParameterSetName = ContextParameterSetName)]
    [Alias("ThrottleLimit")]
    [ValidateRange(1, int.MaxValue)]
    public int Parallelism
    {
        get => _parallelism > 0 ? _parallelism : _parallelism = Environment.ProcessorCount;
        set
        {
            if (value <= 0)
                throw new ArgumentOutOfRangeException(nameof(value));

            _parallelism = value;
        }
    }

    protected override void BeginProcessing()
    {
        _hostFactory = new(Host);

        _previousSynchronizationContext = SynchronizationContext.Current;
        SynchronizationContext.SetSynchronizationContext(null);
    }

    protected override void ProcessRecord()
    {
        if (_cancellation.IsCancellationRequested)
            return;

        if (ParameterSetName == ContextParameterSetName)
            ProcessContextSet(new() { Contexts = Context, Parallelism = Parallelism });
        else
            foreach (var contextSet in Target)
                ProcessContextSet(contextSet);
    }

    private void ProcessContextSet(SqlContextParallelSet contextSet)
    {
        if (contextSet.Contexts.Count == 0)
            return;

        Task ProcessAsync()
            => ProcessContextSetAsync(contextSet);

        _tasks.Add(Task.Run(ProcessAsync));
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
        await limiter.WaitAsync(_cancellation.Token);
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
            _cancellation.Cancel();
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
            "CancellationToken", _cancellation.Token, null
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

        _dispatcher.Post(() => WriteOutput(item));
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

    protected override void StopProcessing()
    {
        // Invoked when a running command needs to be stopped, such as when
        // the user presses CTRL-C.  Invoked on a different thread than the
        // Begin/Process/End sequence.

        Host.UI.WriteWarningLine("Canceling...");
        _cancellation.Cancel();
    }

    protected override void EndProcessing()
    {
        try
        {
            if (_tasks.Count == 0)
                return;

            Task.WhenAll(_tasks).ContinueWith(_ => _dispatcher.Complete());

            _dispatcher.Run();

            ThrowCollectedExceptions();
        }
        finally
        {
            SynchronizationContext.SetSynchronizationContext(_previousSynchronizationContext);
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

    /// <inheritdoc/>
    public void Dispose()
    {
        _cancellation.Dispose();
        _dispatcher  .Dispose();
    }
}
