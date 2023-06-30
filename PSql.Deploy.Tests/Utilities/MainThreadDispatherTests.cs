// Copyright 2023 Subatomix Research Inc.
// SPDX-License-Identifier: ISC

namespace PSql.Deploy;

[TestFixture]
public class MainThreadDispatherTests
{
    [Test]
    public void Run_WrongThread()
    {
        using var dispatcher = new MainThreadDispatcher();

        dispatcher
            .Invoking(d => Task.Run(d.Run).GetAwaiter().GetResult())
            .Should().Throw<InvalidOperationException>()
            .WithMessage("This method must be invoked from the thread that constructed the dispatcher.");
    }

    [Test]
    public void Post_WithoutRun()
    {
        using var dispatcher = new MainThreadDispatcher();

        var mainThreadId = 0;

        dispatcher.Post(() => mainThreadId = CurrentThreadId);

        mainThreadId.Should().Be(CurrentThreadId);
    }

    [Test]
    public void Post_DuringRun()
    {
        using var dispatcher = new MainThreadDispatcher();

        var mainThreadId = 0;
        var taskThreadId = 0;

        var task = Task.Run(() =>
        {
            taskThreadId = CurrentThreadId;
            dispatcher.Post(() => mainThreadId = CurrentThreadId);
            dispatcher.End();
        });

        dispatcher.Run();
        task.Wait();

        mainThreadId.Should().Be(CurrentThreadId);
        taskThreadId.Should().NotBe(CurrentThreadId).And.NotBe(0);
    }

    [Test]
    public void Post_DuringRun_Nested()
    {
        using var dispatcher = new MainThreadDispatcher();

        var mainThreadId = 0;
        var taskThreadId = 0;

        var task = Task.Run(() =>
        {
            taskThreadId = CurrentThreadId;
            dispatcher.Post(() => dispatcher.Post(() => mainThreadId = CurrentThreadId));
            dispatcher.End();
        });

        dispatcher.Run();
        task.Wait();

        mainThreadId.Should().Be(CurrentThreadId);
        taskThreadId.Should().NotBe(CurrentThreadId).And.NotBe(0);
    }


    private static int CurrentThreadId => Thread.CurrentThread.ManagedThreadId;
}
