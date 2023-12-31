// Copyright 2023 Subatomix Research Inc.
// SPDX-License-Identifier: ISC

using PSql.Internal;

namespace PSql.Deploy;

[TestFixture]
public class NullSqlConnectionTests
{
    private readonly ISqlConnection
        Connection = new NullSqlConnection();

    [Test]
    public void UnderlyingConnection_Get()
    {
        Connection
            .Invoking(c => c.UnderlyingConnection)
            .Should().Throw<NotSupportedException>();
    }

    [Test]
    public void ConnectionString_Get()
    {
        Connection.ConnectionString.Should().BeEmpty();
    }

    [Test]
    public void IsOpen_Get()
    {
        Connection.IsOpen.Should().BeTrue();
    }

    [Test]
    public void HasErrors_Get()
    {
        Connection.HasErrors.Should().BeFalse();
    }

    [Test]
    public void CreateCommand()
    {
        Connection.CreateCommand().Should().BeOfType<NullSqlCommand>();
    }

    [Test]
    public void ClearErrors()
    {
        Connection.ClearErrors();
    }

    [Test]
    public void ThrowIfHasErrors()
    {
        Connection.ThrowIfHasErrors();
    }

    [Test]
    public void Dispose()
    {
        Connection.Dispose();
    }

    [Test]
    public async ValueTask DisposeAsync()
    {
        await Connection.DisposeAsync();
    }
}
