// Copyright Subatomix Research Inc.
// SPDX-License-Identifier: MIT

namespace PSql.Deploy.Utilities;

[TestFixture]
public class TextWriterSqlMessageLoggerTests
{
    [Test]
    public void Construct_NullWriter()
    {
        Should.Throw<ArgumentNullException>(() =>
        {
            new TextWriterSqlMessageLogger(writer: null!);
        });
    }

    [Test]
    public void LogWarning()
    {
    }
}
