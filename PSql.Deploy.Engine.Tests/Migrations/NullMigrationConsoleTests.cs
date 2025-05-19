// Copyright Subatomix Research Inc.
// SPDX-License-Identifier: MIT

namespace PSql.Deploy.Migrations;

[TestFixture]
[FixtureLifeCycle(LifeCycle.SingleInstance)]
public class NullMigrationConsoleTests
{
    [Test]
    public void ReportStarting()
    {
        NullMigrationConsole.Instance.ReportStarting(
            Mock.Of<IMigrationSession>(),
            new Target("Server=.")
        );
    }

    [Test]
    public void ReportApplying()
    {
        NullMigrationConsole.Instance.ReportApplying(
            Mock.Of<IMigrationSession>(),
            new Target("Server=."),
            "Test",
            MigrationPhase.Pre
        );
    }

    [Test]
    public void ReportApplied()
    {
        NullMigrationConsole.Instance.ReportApplied(
            Mock.Of<IMigrationSession>(),
            new Target("Server=."),
            count: 42,
            TimeSpan.FromMilliseconds(1234),
            TargetDisposition.Successful
        );
    }

    [Test]
    public void ReportProblem()
    {
        NullMigrationConsole.Instance.ReportProblem(
            Mock.Of<IMigrationSession>(),
            new Target("Server=."),
            message: "any"
        );
    }

    public void CreateLog()
    {
        using var log = NullMigrationConsole.Instance.CreateLog(
            Mock.Of<IMigrationSession>(),
            new Target("Server=.")
        );

        log.ShouldBeSameAs(TextWriter.Null);
    }
}
