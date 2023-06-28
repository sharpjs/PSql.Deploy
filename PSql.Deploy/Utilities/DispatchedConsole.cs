// Copyright 2023 Subatomix Research Inc.
// SPDX-License-Identifier: ISC

namespace PSql.Deploy.Utilities;

/// <summary>
///   A wrapper that synchronizes access to an <see cref="IConsole"/> by
///   invoking its members using an <see cref="IDispatcher"/>.
/// </summary>
internal class DispatchedConsole : IConsole
{
    private readonly IConsole    _console;
    private readonly IDispatcher _dispatcher;

    /// <summary>
    ///   Initializes a new <see cref="DispatchedConsole"/> instance.
    /// </summary>
    /// <param name="console">
    ///   The underlying console instance.
    /// </param>
    /// <param name="dispatcher">
    ///   The dispatcher via which to invoke <paramref name="console"/> methods.
    /// </param>
    /// <exception cref="ArgumentNullException">
    ///   <paramref name="console"/> and/or <paramref name="dispatcher"/> is
    ///   <see langword="null"/>.
    /// </exception>
    public DispatchedConsole(IConsole console, IDispatcher dispatcher)
    {
        if (console is null)
            throw new ArgumentNullException(nameof(console));
        if (dispatcher is null)
            throw new ArgumentNullException(nameof(dispatcher));

        _console    = console;
        _dispatcher = dispatcher;
    }

    /// <inheritdoc/>
    public void WriteObject(object obj)
    {
        _dispatcher.Post(() => _console.WriteObject(obj));
    }

    /// <inheritdoc/>
    public void WriteObject(object obj, bool enumerate)
    {
        _dispatcher.Post(() => _console.WriteObject(obj, enumerate));
    }

    /// <inheritdoc/>
    public void WriteError(ErrorRecord errorRecord)
    {
        _dispatcher.Post(() => _console.WriteError(errorRecord));
    }

    /// <inheritdoc/>
    public void WriteWarning(string text)
    {
        _dispatcher.Post(() => _console.WriteWarning(text));
    }

    /// <inheritdoc/>
    public void WriteVerbose(string text)
    {
        _dispatcher.Post(() => _console.WriteVerbose(text));
    }

    /// <inheritdoc/>
    public void WriteDebug(string text)
    {
        _dispatcher.Post(() => _console.WriteDebug(text));
    }

    /// <inheritdoc/>
    public void WriteHost(
        string        text,
        bool          newLine         = true,
        ConsoleColor? foregroundColor = null,
        ConsoleColor? backgroundColor = null)
    {
        _dispatcher.Post(() => _console.WriteHost(text, newLine, foregroundColor, backgroundColor));
    }

    /// <inheritdoc/>
    public void WriteInformation(InformationRecord informationRecord)
    {
        _dispatcher.Post(() => _console.WriteInformation(informationRecord));
    }

    /// <inheritdoc/>
    public void WriteInformation(object messageData, string[] tags)
    {
        _dispatcher.Post(() => _console.WriteInformation(messageData, tags));
    }

    /// <inheritdoc/>
    public void WriteProgress(ProgressRecord progressRecord)
    {
        _dispatcher.Post(() => _console.WriteProgress(progressRecord));
    }

    /// <inheritdoc/>
    public void WriteCommandDetail(string text)
    {
        _dispatcher.Post(() => _console.WriteCommandDetail(text));
    }

    /// <inheritdoc/>
    public bool ShouldContinue(string query, string caption)
    {
        var tcs = new TaskCompletionSource<bool>();

        _dispatcher.Post(() => tcs.SetResult(
            _console.ShouldContinue(query, caption)
        ));

        return tcs.Task.GetAwaiter().GetResult();
    }

    /// <inheritdoc/>
    public bool ShouldContinue(
        string   query,
        string   caption,
        ref bool yesToAll,
        ref bool noToAll)
    {
        var tcs       = new TaskCompletionSource<bool>();
        var yesToAll_ = yesToAll;
        var noToAll_  = noToAll;

        _dispatcher.Post(() =>
        {
            var yesToAll = yesToAll_;
            var noToAll  = noToAll_;
            var result   = _console.ShouldContinue(query, caption, ref yesToAll, ref noToAll);
            yesToAll_    = yesToAll;
            noToAll_     = noToAll;
            tcs.SetResult(result);
        });

        var result = tcs.Task.GetAwaiter().GetResult();
        yesToAll   = yesToAll_;
        noToAll    = noToAll_;
        return result;
    }

    /// <inheritdoc/>
    public bool ShouldContinue(
        string   query,
        string   caption,
        bool     hasSecurityImpact,
        ref bool yesToAll,
        ref bool noToAll)
    {
        var tcs       = new TaskCompletionSource<bool>();
        var yesToAll_ = yesToAll;
        var noToAll_  = noToAll;

        _dispatcher.Post(() =>
        {
            var yesToAll = yesToAll_;
            var noToAll  = noToAll_;
            var impact   = hasSecurityImpact;
            var result   = _console.ShouldContinue(query, caption, impact, ref yesToAll, ref noToAll);
            yesToAll_    = yesToAll;
            noToAll_     = noToAll;
        });

        var result = tcs.Task.GetAwaiter().GetResult();
        yesToAll   = yesToAll_;
        noToAll    = noToAll_;
        return result;
    }

    /// <inheritdoc/>
    public bool ShouldProcess(string target)
    {
        var tcs = new TaskCompletionSource<bool>();

        _dispatcher.Post(() => tcs.SetResult(
            _console.ShouldProcess(target)
        ));

        return tcs.Task.GetAwaiter().GetResult();
    }

    /// <inheritdoc/>
    public bool ShouldProcess(string target, string action)
    {
        var tcs = new TaskCompletionSource<bool>();

        _dispatcher.Post(() => tcs.SetResult(
            _console.ShouldProcess(target, action)
        ));

        return tcs.Task.GetAwaiter().GetResult();
    }

    /// <inheritdoc/>
    public bool ShouldProcess(string verboseDescription, string verboseWarning, string caption)
    {
        var tcs = new TaskCompletionSource<bool>();

        _dispatcher.Post(() => tcs.SetResult(
            _console.ShouldProcess(verboseDescription, verboseWarning, caption)
        ));

        return tcs.Task.GetAwaiter().GetResult();
    }

    /// <inheritdoc/>
    public bool ShouldProcess(
        string                  verboseDescription,
        string                  verboseWarning,
        string                  caption,
        out ShouldProcessReason shouldProcessReason)
    {
        var tcs     = new TaskCompletionSource<bool>();
        var reason_ = default(ShouldProcessReason);

        _dispatcher.Post(() =>
        {
            var result = _console.ShouldProcess(
                verboseDescription, verboseWarning, caption, out var reason
            );
            reason_ = reason;
            tcs.SetResult(result);
        });

        var result = tcs.Task.GetAwaiter().GetResult();
        shouldProcessReason = reason_;
        return result;
    }

    /// <inheritdoc/>
    public void ThrowTerminatingError(ErrorRecord errorRecord)
    {
        _dispatcher.Post(() => _console.ThrowTerminatingError(errorRecord));
    }
}
