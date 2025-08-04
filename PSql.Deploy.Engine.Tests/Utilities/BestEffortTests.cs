// Copyright Subatomix Research Inc.
// SPDX-License-Identifier: MIT

namespace PSql.Deploy.Utilities;

[TestFixture]
public class BestEffortTests : TestHarnessBase
{
    [Test]
    public void Do_NullAction()
    {
        BestEffort.Do(action: null!, 42);
    }

    [Test]
    public void Do_Ok()
    {
        var action = Mocks.Create<Action<int>>();

        action.Setup(a => a(42)).Verifiable();

        BestEffort.Do(action.Object, 42);
    }

    [Test]
    public void Do_Exception()
    {
        var action = Mocks.Create<Action<int>>();

        action.Setup(a => a(42)).Throws(new Exception()).Verifiable();

        BestEffort.Do(action.Object, 42);
    }

    [Test]
    public async Task DoAsync_NullAction()
    {
        await BestEffort.DoAsync(action: null!, 42);
    }

    [Test]
    public async Task DoAsync_Ok()
    {
        var action = Mocks.Create<Func<int, ValueTask>>();

        action.Setup(a => a(42)).Returns(ValueTask.CompletedTask).Verifiable();

        await BestEffort.DoAsync(action.Object, 42);
    }

    [Test]
    public async Task DoAsync_Exception()
    {
        var action = Mocks.Create<Func<int, ValueTask>>();

        action.Setup(a => a(42)).ThrowsAsync(new Exception()).Verifiable();

        await BestEffort.DoAsync(action.Object, 42);
    }
}
