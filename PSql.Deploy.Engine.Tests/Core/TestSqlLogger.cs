// Copyright Subatomix Research Inc.
// SPDX-License-Identifier: MIT

using System.Diagnostics;

namespace PSql.Deploy;

internal class TestSqlLogger : ISqlMessageLogger
{
    public static TestSqlLogger Instance { get; } = new();

    public void Log(string procedure, int line, int number, int severity, string? message)
    {
        message = $"{procedure}:{line}: E{number}:{severity}: {message}";

        Debug.WriteLine(message);
        TestContext.WriteLine(message);
        Console.WriteLine(message);
    }
}
