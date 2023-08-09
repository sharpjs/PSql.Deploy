// Copyright 2023 Subatomix Research Inc.
// SPDX-License-Identifier: ISC

using PSql.Deploy.Utilities;

namespace PSql.Deploy.Commands;

/// <summary>
///   Base class for PowerShell cmdlets that can execute asynchronous code.
/// </summary>
public abstract class AsyncPSCmdlet : PSCmdlet, ICmdlet, IDisposable
{
    private AsyncCmdletScope? _asyncScope;

    /// <inheritdoc cref="AsyncCmdletScope.Dispatcher"/>
    public IDispatcher Dispatcher
        => _asyncScope?.Dispatcher ?? ImmediateDispatcher.Instance;

    /// <inheritdoc cref="AsyncCmdletScope.CancellationToken"/>
    public CancellationToken CancellationToken
        => _asyncScope?.CancellationToken ?? default;

    /// <inheritdoc/>
    protected override void BeginProcessing()
    {
        _asyncScope = new(this);
    }

    /// <inheritdoc/>
    protected override void EndProcessing()
    {
        _asyncScope?.Complete();
    }

    /// <inheritdoc/>
    protected override void StopProcessing()
    {
        // Invoked when a running command needs to be stopped, such as when
        // the user presses CTRL-C.  Invoked on a different thread than the
        // Begin/Process/End sequence.

        _asyncScope?.Cancel();
    }

    /// <inheritdoc cref="AsyncCmdletScope.Run"/>
    /// <exception cref="InvalidOperationException">
    ///   <see cref="BeginProcessing"/> has not been invoked.
    /// </exception>
    protected void Run(Func<Task> action)
    {
        if (_asyncScope is not { } scope)
            throw new InvalidOperationException(
                "This method requires prior invocation of BeginProcessing."
            );

        // If some prior invocation of Run has already dispatched actions to
        // the main thread, run them now.
        InvokePendingMainThreadActions();

        scope.Run(action);
    }

    /// <summary>
    ///   Invokes any pending actions dispatched to the main thread.
    /// </summary>
    protected void InvokePendingMainThreadActions()
    {
        _asyncScope?.InvokePendingMainThreadActions();
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        Dispose(managed: true);
        GC.SuppressFinalize(this);
    }

    /// <inheritdoc cref="Dispose()"/>
    protected virtual void Dispose(bool managed)
    {
        if (!managed)
            return;

        _asyncScope?.Dispose();
    }

    #region PSCmdlet/ICmdlet (Re)Implementation

    /// <inheritdoc/>
    public new void WriteObject(object? obj)
    {
        Dispatcher.Post(() => base.WriteObject(obj));
    }

    /// <inheritdoc/>
    public new void WriteObject(object? obj, bool enumerate)
    {
        Dispatcher.Post(() => base.WriteObject(obj, enumerate));
    }

    /// <inheritdoc/>
    public new void WriteError(ErrorRecord record)
    {
        static void WriteError((PSCmdlet cmdlet, ErrorRecord record) x)
            => x.cmdlet.WriteError(x.record);

        Dispatcher.Invoke(WriteError, ((PSCmdlet) this, record));
    }

    /// <inheritdoc/>
    public new void WriteWarning(string? text)
    {
        static void WriteWarning((PSCmdlet cmdlet, string? text) x)
            => x.cmdlet.WriteWarning(x.text);

        Dispatcher.Invoke(WriteWarning, ((PSCmdlet) this, text));
    }

    /// <inheritdoc/>
    public new void WriteVerbose(string? text)
    {
        static void WriteVerbose((PSCmdlet cmdlet, string? text) x)
            => x.cmdlet.WriteVerbose(x.text);

        Dispatcher.Invoke(WriteVerbose, ((PSCmdlet) this, text));
    }

    /// <inheritdoc/>
    public new void WriteDebug(string? text)
    {
        static void WriteDebug((PSCmdlet cmdlet, string? text) x)
            => x.cmdlet.WriteDebug(x.text);

        Dispatcher.Invoke(WriteDebug, ((PSCmdlet) this, text));
    }

    /// <inheritdoc/>
    public void WriteHost(
        string?       text,
        bool          newLine         = true,
        ConsoleColor? foregroundColor = null,
        ConsoleColor? backgroundColor = null)
    {
        // This will invoke WriteInformation below
        ((PSCmdlet) this).WriteHost(text!, newLine, foregroundColor, backgroundColor);
    }

    /// <inheritdoc/>
    public new void WriteInformation(InformationRecord record)
    {
        static void WriteInformation((PSCmdlet cmdlet, InformationRecord record) x)
            => x.cmdlet.WriteInformation(x.record);

        Dispatcher.Invoke(WriteInformation, ((PSCmdlet) this, record));
    }

    /// <inheritdoc/>
    public new void WriteInformation(object? data, string?[]? tags)
    {
        static void WriteInformation((PSCmdlet cmdlet, object? data, string?[]? tags) x)
            => x.cmdlet.WriteInformation(x.data, x.tags);

        Dispatcher.Invoke(WriteInformation, ((PSCmdlet) this, data, tags));
    }

    /// <inheritdoc/>
    public new void WriteProgress(ProgressRecord record)
    {
        static void WriteProgress((PSCmdlet cmdlet, ProgressRecord record) x)
            => x.cmdlet.WriteProgress(x.record);

        Dispatcher.Invoke(WriteProgress, ((PSCmdlet) this, record));
    }

    /// <inheritdoc/>
    public new void WriteCommandDetail(string? text)
    {
        static void WriteCommandDetail((PSCmdlet cmdlet, string? text) x)
            => x.cmdlet.WriteCommandDetail(x.text);

        Dispatcher.Invoke(WriteCommandDetail, ((PSCmdlet) this, text));
    }

    /// <inheritdoc/>
    public new bool ShouldContinue(string? query, string? caption)
    {
        static bool ShouldContinue((PSCmdlet cmdlet, string? query, string? caption) x)
            => x.cmdlet.ShouldContinue(x.query, x.caption);

        return Dispatcher.Invoke(ShouldContinue, ((PSCmdlet) this, query, caption));
    }

    /// <inheritdoc/>
    public new bool ShouldContinue(
        string?  query,
        string?  caption,
        ref bool yesToAll,
        ref bool noToAll)
    {
        static (bool, bool, bool) ShouldContinue((
            PSCmdlet cmdlet,
            string?  query,
            string?  caption,
            bool     yesToAll,
            bool     noToAll) x)
        {
            return (
                x.cmdlet.ShouldContinue(
                    x.query, x.caption, ref x.yesToAll, ref x.noToAll
                ),
                x.yesToAll,
                x.noToAll
            );
        }

        (var result, yesToAll, noToAll) = Dispatcher.Invoke(
            ShouldContinue, ((PSCmdlet) this, query, caption, yesToAll, noToAll)
        );

        return result;
    }

    /// <inheritdoc/>
    public new bool ShouldContinue(
        string?  query,
        string?  caption,
        bool     hasSecurityImpact,
        ref bool yesToAll,
        ref bool noToAll)
    {
        static (bool, bool, bool) ShouldContinue((
            PSCmdlet cmdlet,
            string?  query,
            string?  caption,
            bool     hasSecurityImpact,
            bool     yesToAll,
            bool     noToAll) x)
        {
            return (
                x.cmdlet.ShouldContinue(
                    x.query, x.caption, x.hasSecurityImpact, ref x.yesToAll, ref x.noToAll
                ),
                x.yesToAll,
                x.noToAll
            );
        }

        (var result, yesToAll, noToAll) = Dispatcher.Invoke(
            ShouldContinue, ((PSCmdlet) this, query, caption, hasSecurityImpact, yesToAll, noToAll)
        );

        return result;
    }

    /// <inheritdoc/>
    public new bool ShouldProcess(string? target)
    {
        static bool ShouldProcess((PSCmdlet cmdlet, string? target) x)
            => x.cmdlet.ShouldProcess(x.target);

        return Dispatcher.Invoke(ShouldProcess, ((PSCmdlet) this, target));
    }

    /// <inheritdoc/>
    public new bool ShouldProcess(string? target, string? action)
    {
        static bool ShouldProcess((PSCmdlet cmdlet, string? target, string? action) x)
            => x.cmdlet.ShouldProcess(x.target, x.action);

        return Dispatcher.Invoke(ShouldProcess, ((PSCmdlet) this, target, action));
    }

    /// <inheritdoc/>
    public new bool ShouldProcess(string? verboseDescription, string? verboseWarning, string? caption)
    {
        static bool ShouldProcess((
            PSCmdlet cmdlet,
            string?  verboseDescription,
            string?  verboseWarning,
            string?  caption) x)
        {
            return x.cmdlet.ShouldProcess(
                x.verboseDescription, x.verboseWarning, x.caption
            );
        }

        return Dispatcher.Invoke(
            ShouldProcess, ((PSCmdlet) this, verboseDescription, verboseWarning, caption)
        );
    }

    /// <inheritdoc/>
    public new bool ShouldProcess(
        string?                 verboseDescription,
        string?                 verboseWarning,
        string?                 caption,
        out ShouldProcessReason shouldProcessReason)
    {
        static (bool, ShouldProcessReason) ShouldProcess((
            PSCmdlet cmdlet,
            string?  verboseDescription,
            string?  verboseWarning,
            string?  caption) x)
        {
            return (
                x.cmdlet.ShouldProcess(
                    x.verboseDescription, x.verboseWarning, x.caption, out var reason
                ),
                reason
            );
        }

        (var result, shouldProcessReason) = Dispatcher.Invoke(
            ShouldProcess, ((PSCmdlet) this, verboseDescription, verboseWarning, caption)
        );

        return result;
    }

    /// <inheritdoc/>
    public new void ThrowTerminatingError(ErrorRecord errorRecord)
    {
        Dispatcher.Post(() => base.ThrowTerminatingError(errorRecord));
    }

    #endregion
}
