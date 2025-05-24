// Copyright Subatomix Research Inc.
// SPDX-License-Identifier: MIT

using System.Net;

namespace PSql.Deploy;

[TestFixture]
public class SqlTargetConnectionTests
{
    [Test]
    public void Construct_NullTarget()
    {
        Should.Throw<ArgumentNullException>(() =>
        {
            _ = new TestSqlTargetConnection(null!, TestSqlLogger.Instance);
        });
    }

    [Test]
    public void Construct_NullLogger()
    {
        Should.Throw<ArgumentNullException>(() =>
        {
            _ = new TestSqlTargetConnection(new Target("Server = ."), null!);
        });
    }

    [Test]
    public void Target_Get()
    {
        var target = new Target("Server = .");

        using var connection = new TestSqlTargetConnection(target, TestSqlLogger.Instance);

        connection.Target.ShouldBeSameAs(target);
    }

    [Test]
    public void Logger_Get()
    {
        var target = new Target("Server = .");

        using var connection = new TestSqlTargetConnection(target, TestSqlLogger.Instance);

        connection.Logger.ShouldBeSameAs(TestSqlLogger.Instance);
    }

    [Test]
    public void Connection_Get_WithoutCredential()
    {
        var target = new Target("Server = .");

        using var connection = new TestSqlTargetConnection(target, TestSqlLogger.Instance);

        connection.Connection           .ShouldNotBeNull();
        connection.Connection.Credential.ShouldBeNull();
    }

    [Test]
    public void Construct_WithCredential()
    {
        var input  = new NetworkCredential("user", "password");
        var target = new Target("Server = .", credential: input);

        using var connection = new TestSqlTargetConnection(target, TestSqlLogger.Instance);

        connection.Connection           .ShouldNotBeNull();
        connection.Connection.Credential.ShouldNotBeNull().AssignTo(out var actual);

        actual.ShouldNotBeNull();

        var output = new NetworkCredential(actual.UserId, actual.Password);

        output.UserName.ShouldBe(input.UserName);
        output.Password.ShouldBe(input.Password);
    }

    [Test]
    public void SetUpCommand_NegativeTimeout()
    {
        var target = new Target("Server = .");

        using var connection = new TestSqlTargetConnection(target, TestSqlLogger.Instance);

        Should.Throw<ArgumentOutOfRangeException>(() =>
        {
            connection.SetUpCommandWithNegativeTimeout();
        });
    }

    [Test]
    public void Dispose()
    {
        var target = new Target("Server = .");

        using var connection = new TestSqlTargetConnection(target, TestSqlLogger.Instance);

        connection.Dispose(); // To test multiple disposal
    }

    [Test]
    public async Task DisposeAsync()
    {
        var target = new Target("Server = .");

        await using var connection = new TestSqlTargetConnection(target, TestSqlLogger.Instance);

        await connection.DisposeAsync(); // To test multiple disposal
    }

    [Test]
    public void HandleUnexpectedDisposal()
    {
        var target = new Target("Server = .");

        using var connection = new TestSqlTargetConnection(target, TestSqlLogger.Instance);

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
