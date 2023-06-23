// Copyright 2023 Subatomix Research Inc.
// SPDX-License-Identifier: ISC

namespace PSql.Deploy;

internal sealed class ConsoleCancellationListener : IDisposable
{
    private readonly CancellationTokenSource _cts;

    public ConsoleCancellationListener(CancellationTokenSource cts)
    {
        if (cts is null)
            throw new ArgumentNullException(nameof(cts));

        _cts = cts;

        Console.CancelKeyPress += Console_CancelKeyPress;
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        Console.CancelKeyPress -= Console_CancelKeyPress;
    }

    private void Console_CancelKeyPress(object sender, ConsoleCancelEventArgs e)
    {
        _cts.Cancel();
    }
}
