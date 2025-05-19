// Copyright Subatomix Research Inc.
// SPDX-License-Identifier: MIT

namespace PSql.Deploy;

[TestFixture]
public class WhatIfTargetConnectionTests : TestHarnessBase
{
    private readonly TestWhatIfTargetConnection _outer;
    private readonly Mock<ITargetConnection>    _inner;
    private readonly Mock<ISqlMessageLogger>    _logger;

    public WhatIfTargetConnectionTests()
    {
        _logger = Mocks.Create<ISqlMessageLogger>();
        _inner  = Mocks.Create<ITargetConnection>();
        _outer  = new(_inner.Object);
    }

    [Test]
    public void Construct_NullConnection()
    {
        Should.Throw<ArgumentNullException>(() =>
        {
            _ = new TestWhatIfTargetConnection(null!);
        });
    }

    [Test]
    public void UnderlyingConnection_Get()
    {
        _outer.UnderlyingConnection.ShouldBeSameAs(_inner.Object);
    }

    [Test]
    public void Target_Get()
    {
        var target = new Target("Server = .");

        _inner.Setup(c => c.Target).Returns(target);

        _outer.Target.ShouldBeSameAs(target);
    }

    [Test]
    public void Logger_Get()
    {
        _inner.Setup(c => c.Logger).Returns(_logger.Object);

        _outer.Logger.ShouldBeSameAs(_logger.Object);
    }

    [Test]
    public async Task OpenAsync()
    {
        _inner
            .Setup(c => c.OpenAsync(Cancellation.Token))
            .Returns(Task.CompletedTask)
            .Verifiable();

        await _outer.OpenAsync(Cancellation.Token);
    }

    [Test]
    public void Dispose_()
    {
        _inner
            .Setup(c => c.Dispose())
            .Verifiable();

        _outer.Dispose();
    }

    [Test]
    public async Task DisposeAsync()
    {
        _inner
            .Setup(c => c.DisposeAsync())
            .Returns(ValueTask.CompletedTask)
            .Verifiable();

        await _outer.DisposeAsync();
    }

    [Test]
    public void Log()
    {
        _inner
            .Setup(c => c.Logger.Log("", 0, 0, 0, "a"))
            .Verifiable();

        _outer.Log("a");
    }

    private class TestWhatIfTargetConnection : WhatIfTargetConnection
    {
        public TestWhatIfTargetConnection(ITargetConnection connection)
            : base(connection) { }

        public new ITargetConnection UnderlyingConnection
            => base.UnderlyingConnection;

        public new void Log(string? message)
            => base.Log(message);
    }
}
