// Copyright Subatomix Research Inc.
// SPDX-License-Identifier: MIT

namespace PSql.Deploy;

using static SqlMessageConstants;

[TestFixture]
public class PrefixTextWriterSqlMessageLoggerTests
{
    private readonly PrefixTextWriterSqlMessageLogger _logger;
    private readonly StringWriter                     _writer;

    public PrefixTextWriterSqlMessageLoggerTests()
    {
        _writer = new();
        _logger = new(_writer, prefix: "Test>");
    }

    [Test]
    public void Construct_NullWriter()
    {
        Should.Throw<ArgumentNullException>(() =>
        {
            _ = new PrefixTextWriterSqlMessageLogger(null!, "any");
        });
    }

    [Test]
    public void Construct_NullPrefix()
    {
        Should.Throw<ArgumentNullException>(() =>
        {
            _ = new PrefixTextWriterSqlMessageLogger(_writer, null!);
        });
    }

    [Test]
    public void Log_Information()
    {
        _logger.Log("foo", line: 42, number: 1337, MaxInformationalSeverity, message: "a");

        _writer.ToString().ShouldBe("Test> a" + Environment.NewLine);
    }

    [Test]
    public void Log_Error()
    {
        (11).ShouldBeGreaterThan(MaxInformationalSeverity);

        _logger.Log("foo", line: 42, number: 1337, severity: 11, "a");

        _writer.ToString().ShouldBe("Test> foo:42: E1337:11: a" + Environment.NewLine);
    }
}
