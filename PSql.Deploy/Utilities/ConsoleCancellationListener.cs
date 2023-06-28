// Copyright 2023 Subatomix Research Inc.
// SPDX-License-Identifier: ISC

namespace PSql.Deploy;

internal sealed class ConsoleCancellationListener : IDisposable
{
    private readonly IConsole _console;
    private readonly CancellationTokenSource _cancellation;

    public ConsoleCancellationListener(IConsole console, CancellationTokenSource cancellation)
    {
        if (console is null)
            throw new ArgumentNullException(nameof(console));
        if (cancellation is null)
            throw new ArgumentNullException(nameof(cancellation));

        _console      = console;
        _cancellation = cancellation;

        Console.CancelKeyPress += Console_CancelKeyPress;
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        Console.CancelKeyPress -= Console_CancelKeyPress;
    }

    private void Console_CancelKeyPress(object sender, ConsoleCancelEventArgs e)
    {
        _console.WriteHost("Cancelling...", foregroundColor: ConsoleColor.Yellow);
        _cancellation.Cancel();
    }
}
