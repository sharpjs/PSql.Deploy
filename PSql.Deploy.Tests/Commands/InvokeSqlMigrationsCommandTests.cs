// Copyright Subatomix Research Inc.
// SPDX-License-Identifier: MIT

namespace PSql.Deploy.Commands;

[TestFixture]
public class InvokeSqlMigrationsCommandTests
{
    // More tests in InvokeSqlMigrationsCommandIntegrationTests

    [Test]
    public void Dispose_BeforeBeginProcessing()
    {
        using var command = new InvokeSqlMigrationsCommand();

        // Test both Dispose before BeginProcessing and multiple disposal
        command.Dispose();
    }

    [Test]
    public void AssumeBeginProcessingInvoked()
    {
        using var command = new InvokeSqlMigrationsCommand();

#if DEBUG
        Should.Throw<InvalidOperationException>(() => command.AssumeBeginProcessingInvoked());
#else
        // Method is a NOP in non-debug builds
        Assert.Pass();
#endif
    }
}
