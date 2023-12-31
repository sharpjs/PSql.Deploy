// Copyright 2023 Subatomix Research Inc.
// SPDX-License-Identifier: ISC

namespace PSql.Deploy.Seeding;

[TestFixture]
public class SeedSqlMessageLoggerTests
{
    [Test]
    public void LogInformation()
    {
        using var writer = new StringWriter();

        var logger = new SeedSqlMessageLogger(writer, workerId: 42);

        logger.LogInformation("foo");

        writer.ToString().Should().Be("42> foo" + writer.NewLine);
    }

    [Test]
    public void LogError()
    {
        using var writer = new StringWriter();

        var logger = new SeedSqlMessageLogger(writer, workerId: 42);

        logger.LogError("foo");

        writer.ToString().Should().Be("42> WARNING: foo" + writer.NewLine);
    }
}
