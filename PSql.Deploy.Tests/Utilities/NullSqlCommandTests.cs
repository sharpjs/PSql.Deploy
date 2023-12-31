// Copyright 2023 Subatomix Research Inc.
// SPDX-License-Identifier: ISC

using PSql.Internal;

namespace PSql.Deploy.Utilities;

[TestFixture]
public class NullSqlCommandTests
{
    private readonly ISqlCommand
        Command = new NullSqlCommand();

    [Test]
    public void UnderlyingCommand_Get()
    {
        Command
            .Invoking(c => c.UnderlyingCommand)
            .Should().Throw<NotSupportedException>();
    }

    [Test]
    public void CommandText_Get()
    {
        Command.CommandText.Should().BeEmpty();
    }

    [Test]
    public void CommandText_Set()
    {
        Command.CommandText = "foo";
        Command.CommandText.Should().Be("foo");
    }

    [Test]
    public void CommandTimeout_Get()
    {
        Command.CommandTimeout.Should().Be(30);
    }

    [Test]
    public void CommandTimeout_Set()
    {
        Command.CommandTimeout = 42;
        Command.CommandTimeout.Should().Be(42);
    }

    [Test]
    public void Dispose()
    {
        Command.Dispose();
    }

    [Test]
    public async ValueTask DisposeAsync()
    {
        await Command.DisposeAsync();
    }
}
