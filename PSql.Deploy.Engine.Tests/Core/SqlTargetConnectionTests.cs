// Copyright Subatomix Research Inc.
// SPDX-License-Identifier: MIT

using System.Net;

namespace PSql.Deploy;

[TestFixture]
public class SqlTargetConnectionTests
{
    private readonly Target _target               = new("Server=.");
    private readonly Target _targetWithCredential = new("Server=.", new("username", "password"));

    private readonly ISqlMessageLogger _logger = TestSqlLogger.Instance;

    [Test]
    public void Construct_NullTarget()
    {
        Should.Throw<ArgumentNullException>(() =>
        {
            _ = new TestSqlTargetConnection(null!, _logger);
        });
    }

    [Test]
    public void Construct_NullLogger()
    {
        Should.Throw<ArgumentNullException>(() =>
        {
            _ = new TestSqlTargetConnection(_target, null!);
        });
    }

    [Test]
    public void Target_Get()
    {
        using var connection = new TestSqlTargetConnection(_target, _logger);

        connection.Target.ShouldBeSameAs(_target);
    }

    [Test]
    public void Logger_Get()
    {
        using var connection = new TestSqlTargetConnection(_target, _logger);

        connection.Logger.ShouldBeSameAs(_logger);
    }

    [Test]
    public void Connection_Get_WithoutCredential()
    {
        using var connection = new TestSqlTargetConnection(_target, _logger);

        connection.Connection           .ShouldNotBeNull();
        connection.Connection.Credential.ShouldBeNull();
    }

    [Test]
    public void Construct_WithCredential()
    {
        using var connection = new TestSqlTargetConnection(_targetWithCredential, _logger);

        connection.Connection           .ShouldNotBeNull();
        connection.Connection.Credential.ShouldNotBeNull().AssignTo(out var actual);

        actual.ShouldNotBeNull();

        var input  = _targetWithCredential.Credential!;
        var output = new NetworkCredential(actual.UserId, actual.Password);

        output.UserName.ShouldBe(input.UserName);
        output.Password.ShouldBe(input.Password);
    }

    [Test]
    public void SetUpCommand_NegativeTimeout()
    {
        using var connection = new TestSqlTargetConnection(_target, _logger);

        Should.Throw<ArgumentOutOfRangeException>(() =>
        {
            connection.SetUpCommandWithNegativeTimeout();
        });
    }

    [Test]
    public void Dispose()
    {
        using var connection = new TestSqlTargetConnection(_target, _logger);

        connection.Dispose(); // To test multiple disposal
    }

    [Test]
    public async Task DisposeAsync()
    {
        await using var connection = new TestSqlTargetConnection(_target, _logger);

        await connection.DisposeAsync(); // To test multiple disposal
    }

    [Test]
    public void HandleUnexpectedDisposal()
    {
        using var connection = new TestSqlTargetConnection(_target, _logger);

        Should.Throw<DataException>(() =>
        {
            connection.Connection.Dispose(); // Simulate unexpected disposal
        })
        .Message.ShouldBe("The connection to the database server was closed unexpectedly.");
    }

    private class TestSqlTargetConnection : SqlTargetConnection
    {
        public TestSqlTargetConnection(Target target, ISqlMessageLogger logger)
            : base(target, logger) { }

        public new SqlConnection Connection
            => base.Connection;

        internal void SetUpCommandWithNegativeTimeout()
            => SetUpCommand("any", timeout: -1);
    }
}
