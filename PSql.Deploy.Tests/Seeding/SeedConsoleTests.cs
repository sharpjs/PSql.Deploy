// Copyright 2023 Subatomix Research Inc.
// SPDX-License-Identifier: ISC

using FluentAssertions.Extensions;

namespace PSql.Deploy.Seeding;

[TestFixture]
[FixtureLifeCycle(LifeCycle.InstancePerTestCase)]
public class SeedConsoleTests : TestHarnessBase
{
    public SeedConsoleTests()
    {
        Cmdlet  = Mocks.Create<ICmdlet>();
        Console = new SeedConsole(Cmdlet.Object);
    }

    private Mock<ICmdlet> Cmdlet  { get; }
    private SeedConsole   Console { get; }

    [Test]
    public void Construct_NullCmdlet()
    {
        Invoking(() => new SeedConsole(null!))
            .Should().Throw<ArgumentNullException>();
    }

    [Test]
    public void Cmdlet_Get()
    {
        Console.Cmdlet.Should().BeSameAs(Cmdlet);
    }

    [Test]
    public void ReportStarting()
    {
        Cmdlet
            .Setup(c => c.WriteHost("Starting", true, null, null))
            .Verifiable();

        Console.ReportStarting();
    }

    [Test]
    public void ReportApplying()
    {
        Cmdlet
            .Setup(c => c.WriteHost("Applying MyModule", true, null, null))
            .Verifiable();

        Console.ReportApplying("MyModule");
    }

    [Test]
    public void ReportApplied()
    {
        Cmdlet
            .Setup(c => c.WriteHost("Applied 42 seed(s) in 1,337.000 second(s) [INCOMPLETE]", true, null, null))
            .Verifiable();

        Console.ReportApplied(42, 1337.Seconds(), TargetDisposition.Incomplete);
    }

    [Test]
    public void ReportProblem()
    {
        Cmdlet
            .Setup(c => c.WriteWarning("Ruh-roh!"))
            .Verifiable();

        Console.ReportProblem("Ruh-roh!");
    }
}
