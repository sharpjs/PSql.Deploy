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
            Mock.Of<IMigrationApplication>()
        );
    }

    [Test]
    public void ReportApplying()
    {
        NullMigrationConsole.Instance.ReportApplying(
            Mock.Of<IMigrationApplication>(),
            "Test",
            MigrationPhase.Pre
        );
    }

    [Test]
    public void ReportApplied()
    {
        NullMigrationConsole.Instance.ReportApplied(
            Mock.Of<IMigrationApplication>(),
            count: 42,
            TimeSpan.FromMilliseconds(1234),
            TargetDisposition.Successful
        );
    }

    [Test]
    public void ReportProblem()
    {
        NullMigrationConsole.Instance.ReportProblem(
            Mock.Of<IMigrationApplication>(),
            message: "any"
        );
    }

    [Test]
    public void CreateLog()
    {
        using var log = NullMigrationConsole.Instance.CreateLog(
            Mock.Of<IMigrationApplication>()
        );

        log.ShouldBeSameAs(TextWriter.Null);
    }
}
