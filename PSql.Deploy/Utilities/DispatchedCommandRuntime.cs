// Copyright 2023 Subatomix Research Inc.
// SPDX-License-Identifier: ISC

using System.Management.Automation.Host;

namespace PSql.Deploy.Utilities;

/// <summary>
///   A wrapper that synchronizes access to an <see cref="ICommandRuntime"/> by
///   invoking its members using an <see cref="IDispatcher"/>.
/// </summary>
internal class DispatchedCommandRuntime : ICommandRuntime2
{
    private readonly ICommandRuntime _runtime;
    private readonly IDispatcher     _dispatcher;

    /// <summary>
    ///   Initializes a new <see cref="DispatchedConsole"/> instance.
    /// </summary>
    /// <param name="runtime">
    ///   The underlying runtime instance.
    /// </param>
    /// <param name="dispatcher">
    ///   The dispatcher via which to invoke <paramref name="runtime"/> methods.
    /// </param>
    /// <exception cref="ArgumentNullException">
    ///   <paramref name="runtime"/> and/or <paramref name="dispatcher"/> is
    ///   <see langword="null"/>.
    /// </exception>
    public DispatchedCommandRuntime(ICommandRuntime runtime, IDispatcher dispatcher)
    {
        if (runtime is null)
            throw new ArgumentNullException(nameof(runtime));
        if (dispatcher is null)
            throw new ArgumentNullException(nameof(dispatcher));

        _runtime    = runtime;
        _dispatcher = dispatcher;
    }

    /// <summary>
    ///   Gets the underlying runtime instance.
    /// </summary>
    public ICommandRuntime UnderlyingCommandRuntime
        => _runtime;

    /// <inheritdoc/>
    public PSHost Host
        => _runtime.Host;

    /// <inheritdoc/>
    public PSTransactionContext CurrentPSTransaction
        => _dispatcher.Invoke(() => _runtime.CurrentPSTransaction);

    /// <inheritdoc/>
    public bool TransactionAvailable()
    {
        return _dispatcher.Invoke(_runtime.TransactionAvailable);
    }

    /// <inheritdoc/>
    public void WriteObject(object obj)
    {
        _dispatcher.Post(() => _runtime.WriteObject(obj));
    }

    /// <inheritdoc/>
    public void WriteObject(object obj, bool enumerate)
    {
        _dispatcher.Post(() => _runtime.WriteObject(obj, enumerate));
    }

    /// <inheritdoc/>
    public void WriteError(ErrorRecord errorRecord)
    {
        _dispatcher.Invoke(() => _runtime.WriteError(errorRecord));
    }

    /// <inheritdoc/>
    public void WriteWarning(string text)
    {
        _dispatcher.Invoke(() => _runtime.WriteWarning(text));
    }

    /// <inheritdoc/>
    public void WriteVerbose(string text)
    {
        _dispatcher.Invoke(() => _runtime.WriteVerbose(text));
    }

    /// <inheritdoc/>
    public void WriteDebug(string text)
    {
        _dispatcher.Invoke(() => _runtime.WriteDebug(text));
    }

    /// <inheritdoc/>
    public void WriteInformation(InformationRecord informationRecord)
    {
        var runtime = UpcastRuntime();
        _dispatcher.Invoke(() => runtime.WriteInformation(informationRecord));
    }

    /// <inheritdoc/>
    public void WriteProgress(ProgressRecord progressRecord)
    {
        _dispatcher.Invoke(() => _runtime.WriteProgress(progressRecord));
    }

    /// <inheritdoc/>
    public void WriteProgress(long sourceId, ProgressRecord progressRecord)
    {
        _dispatcher.Invoke(() => _runtime.WriteProgress(sourceId, progressRecord));
    }

    /// <inheritdoc/>
    public void WriteCommandDetail(string text)
    {
        _dispatcher.Post(() => _runtime.WriteCommandDetail(text));
    }

    /// <inheritdoc/>
    public bool ShouldContinue(string query, string caption)
    {
        return _dispatcher.Invoke(() => _runtime.ShouldContinue(query, caption));
    }

    /// <inheritdoc/>
    public bool ShouldContinue(
        string   query,
        string   caption,
        ref bool yesToAll,
        ref bool noToAll)
    {
        (var result, yesToAll, noToAll) = _dispatcher.Invoke(
            ShouldContinue, (_runtime, query, caption, yesToAll, noToAll)
        );

        return result;
    }

    // invoked by above method
    private static (bool, bool, bool) ShouldContinue((
        ICommandRuntime _runtime,
        string          query,
        string          caption,
        bool            yesToAll,
        bool            noToAll) x)
    {
        return (
            x._runtime.ShouldContinue(
                x.query, x.caption, ref x.yesToAll, ref x.noToAll
            ),
            x.yesToAll,
            x.noToAll
        );
    }

    /// <inheritdoc/>
    public bool ShouldContinue(
        string   query,
        string   caption,
        bool     hasSecurityImpact,
        ref bool yesToAll,
        ref bool noToAll)
    {
        (var result, yesToAll, noToAll) = _dispatcher.Invoke(
            ShouldContinue, (UpcastRuntime(), query, caption, hasSecurityImpact, yesToAll, noToAll)
        );

        return result;
    }

    // invoked by above method
    private static (bool, bool, bool) ShouldContinue((
        ICommandRuntime2 _runtime,
        string           query,
        string           caption,
        bool             hasSecurityImpact,
        bool             yesToAll,
        bool             noToAll) x)
    {
        return (
            x._runtime.ShouldContinue(
                x.query, x.caption, x.hasSecurityImpact, ref x.yesToAll, ref x.noToAll
            ),
            x.yesToAll,
            x.noToAll
        );
    }

    /// <inheritdoc/>
    public bool ShouldProcess(string target)
    {
        return _dispatcher.Invoke(() => _runtime.ShouldProcess(target));
    }

    /// <inheritdoc/>
    public bool ShouldProcess(string target, string action)
    {
        return _dispatcher.Invoke(() => _runtime.ShouldProcess(target, action));
    }

    /// <inheritdoc/>
    public bool ShouldProcess(string verboseDescription, string verboseWarning, string caption)
    {
        return _dispatcher.Invoke(() => _runtime.ShouldProcess(
            verboseDescription, verboseWarning, caption
        ));
    }

    /// <inheritdoc/>
    public bool ShouldProcess(
        string                  verboseDescription,
        string                  verboseWarning,
        string                  caption,
        out ShouldProcessReason shouldProcessReason)
    {
        (var result, shouldProcessReason) = _dispatcher.Invoke(
            ShouldProcess, (_runtime, verboseDescription, verboseWarning, caption)
        );

        return result;
    }

    // invoked by above method
    private static (bool, ShouldProcessReason) ShouldProcess((
        ICommandRuntime runtime,
        string          verboseDescription,
        string          verboseWarning,
        string          caption) x)
    {
        return (
            x.runtime.ShouldProcess(
                x.verboseDescription, x.verboseWarning, x.caption, out var reason
            ),
            reason
        );
    }

    /// <inheritdoc/>
    public void ThrowTerminatingError(ErrorRecord errorRecord)
    {
        _dispatcher.Post(() => _runtime.ThrowTerminatingError(errorRecord));
    }

    private ICommandRuntime2 UpcastRuntime()
    {
        return _runtime as ICommandRuntime2 ?? throw new NotImplementedException();
    }
}
