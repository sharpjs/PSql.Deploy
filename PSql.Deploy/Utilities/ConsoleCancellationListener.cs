// Copyright 2023 Subatomix Research Inc.
// SPDX-License-Identifier: ISC

namespace PSql.Deploy;

/// <summary>
///   A listener that requests cancellation of a
///   <see cref="CancellationTokenSource"/> when the user issues the
///   cancellation key sequence (Ctrl+C or Ctrl+Break) at the console.
/// </summary>
internal sealed class ConsoleCancellationListener : IDisposable
{
    private readonly IConsole                _console;
    private readonly CancellationTokenSource _cancellation;

    /// <summary>
    ///   Initializes a new <see cref="ConsoleCancellationListener"/> instance.
    /// </summary>
    /// <param name="console">
    ///   An abstract console via which to emit a <c>Cancelling...</c> message.
    /// </param>
    /// <param name="cancellation">
    ///   The token source that receives the cancellation request.
    /// </param>
    /// <exception cref="ArgumentNullException">
    ///   <paramref name="console"/> and/or
    ///   <paramref name="cancellation"/> is <see langword="null"/>.
    /// </exception>
    public ConsoleCancellationListener(IConsole console, CancellationTokenSource cancellation)
    {
        if (console is null)
            throw new ArgumentNullException(nameof(console));
        if (cancellation is null)
            throw new ArgumentNullException(nameof(cancellation));

        _console      = console;
        _cancellation = cancellation;

        Console.CancelKeyPress += HandleCancelKeyPress;
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        Console.CancelKeyPress -= HandleCancelKeyPress;
    }

    private void HandleCancelKeyPress(object sender, ConsoleCancelEventArgs e)
    {
        _console.WriteHost("Cancelling...", foregroundColor: ConsoleColor.Yellow);
        _cancellation.Cancel();
    }

    internal void SimulateCancelKeyPress()
    {
        // Handler does not use either of its arguments.
        HandleCancelKeyPress(null!, null!);
    }
}
