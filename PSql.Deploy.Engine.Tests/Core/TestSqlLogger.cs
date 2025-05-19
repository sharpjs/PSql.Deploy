// Copyright Subatomix Research Inc.
// SPDX-License-Identifier: MIT

namespace PSql.Deploy;

internal class TestSqlLogger : ISqlMessageLogger
{
    public void Log(string procedure, int line, int number, int severity, string? message)
    {
        TestContext.Out.WriteLine(message);
    }
}
