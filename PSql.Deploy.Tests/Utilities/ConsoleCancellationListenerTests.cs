// Copyright 2023 Subatomix Research Inc.
// SPDX-License-Identifier: ISC

namespace PSql.Deploy.Utilities;

[TestFixture]
public class ConsoleCancellationListenerTests
{
    [Test]
    public void Construct_NullConsole()
    {
        using var cancellation = new CancellationTokenSource();

        Invoking(() => new ConsoleCancellationListener(null!, cancellation))
            .Should().Throw<ArgumentNullException>();
    }

    [Test]
    public void Construct_NullCancellationTokenSource()
    {
        var console = Mock.Of<IConsole>();

        Invoking(() => new ConsoleCancellationListener(console, null!))
            .Should().Throw<ArgumentNullException>();
    }

    [Test]
    public void HandleCancelKeyPress()
    {
        var console = new Mock<IConsole>(MockBehavior.Strict);

        using var cancellation = new CancellationTokenSource();
        using var listener     = new ConsoleCancellationListener(console.Object, cancellation);

        console
            .Setup(c => c.WriteHost("Cancelling...", true, ConsoleColor.Yellow, null))
            .Verifiable();

        listener.SimulateCancelKeyPress();

        console.Verify();
    }
}
