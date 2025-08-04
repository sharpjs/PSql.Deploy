// Copyright Subatomix Research Inc.
// SPDX-License-Identifier: MIT

namespace PSql.Deploy;

using static SqlMessageConstants;

[TestFixture]
public class TextWriterSqlMessageLoggerTests
{
    private readonly TextWriterSqlMessageLogger _logger;
    private readonly StringWriter               _writer;

    public TextWriterSqlMessageLoggerTests()
    {
        _writer = new();
        _logger = new(_writer);
    }

    [Test]
    public void Construct_NullWriter()
    {
        Should.Throw<ArgumentNullException>(() =>
        {
            _ = new TextWriterSqlMessageLogger(null!);
        });
    }

    [Test]
    public void Log_Information()
    {
        _logger.Log("foo", line: 42, number: 1337, MaxInformationalSeverity, message: "a");

        _writer.ToString().ShouldBe("a" + Environment.NewLine);
    }

    [Test]
    public void Log_Error()
    {
        (11).ShouldBeGreaterThan(MaxInformationalSeverity);

        _logger.Log("foo", line: 42, number: 1337, severity: 11, "a");

        _writer.ToString().ShouldBe("foo:42: E1337:11: a" + Environment.NewLine);
    }
}
