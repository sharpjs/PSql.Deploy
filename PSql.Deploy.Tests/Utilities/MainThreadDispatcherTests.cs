// Copyright 2023 Subatomix Research Inc.
// SPDX-License-Identifier: ISC

using PSql.Deploy.Utilities;

namespace PSql.Deploy;

[TestFixture]
public class MainThreadDispatcherTests
{
    [Test]
    public void Post_Null()
    {
        using var dispatcher = new MainThreadDispatcher();

        dispatcher
            .Invoking(d => d.Post(null!))
            .Should().Throw<ArgumentNullException>();
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
            dispatcher.Complete();
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
            dispatcher.Complete();
        });

        dispatcher.Run();
        task.Wait();

        mainThreadId.Should().Be(CurrentThreadId);
        taskThreadId.Should().NotBe(CurrentThreadId).And.NotBe(0);
    }

    [Test]
    public void Post_Completed_OnMainThread()
    {
        using var dispatcher = new MainThreadDispatcher();

        dispatcher.Complete();

        var invoked = false;

        dispatcher.Post(() => invoked = true);

        invoked.Should().BeTrue();
    }

    [Test]
    public void Post_Completed_OnOtherThread()
    {
        using var dispatcher = new MainThreadDispatcher();

        dispatcher.Complete();

        var invoked = false;

        Task.Run(() =>
        {
            dispatcher
                .Invoking(d => d.Post(() => invoked = true))
                .Should().Throw<InvalidOperationException>();
        })
        .GetAwaiter().GetResult();

        invoked.Should().BeFalse();
    }

    [Test]
    public void Post_Disposed_OnMainThread()
    {
        using var dispatcher = new MainThreadDispatcher();

        dispatcher.Dispose();

        var invoked = false;

        dispatcher.Post(() => invoked = true);

        invoked.Should().BeTrue();
    }

    [Test]
    public void Post_Disposed_OnOtherThread()
    {
        using var dispatcher = new MainThreadDispatcher();

        dispatcher.Dispose();

        var invoked = false;

        Task.Run(() =>
        {
            dispatcher
                .Invoking(d => d.Post(() => invoked = true))
                .Should().Throw<ObjectDisposedException>();
        })
        .GetAwaiter().GetResult();

        invoked.Should().BeFalse();
    }

    [Test]
    public void Run_OnOtherThread()
    {
        using var dispatcher = new MainThreadDispatcher();

        dispatcher
            .Invoking(d => Task.Run(d.Run).GetAwaiter().GetResult())
            .Should().Throw<InvalidOperationException>()
            .WithMessage("This method must be invoked from the thread that constructed the dispatcher.");
    }

    [Test]
    public void Run_Completed()
    {
        using var dispatcher = new MainThreadDispatcher();

        dispatcher.Complete();

        dispatcher.Run();
    }

    [Test]
    public void Run_Disposed()
    {
        using var dispatcher = new MainThreadDispatcher();

        dispatcher.Dispose();

        dispatcher
            .Invoking(d => d.Run())
            .Should().Throw<ObjectDisposedException>();
    }

    [Test]
    public void Complete_Disposed()
    {
        using var dispatcher = new MainThreadDispatcher();

        dispatcher.Dispose();

        dispatcher
            .Invoking(d => d.Complete())
            .Should().Throw<ObjectDisposedException>();
    }

    private static int CurrentThreadId => Thread.CurrentThread.ManagedThreadId;
}
