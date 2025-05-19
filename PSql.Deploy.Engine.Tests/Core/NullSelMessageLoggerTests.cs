// Copyright Subatomix Research Inc.
// SPDX-License-Identifier: MIT

namespace PSql.Deploy;

[TestFixture]
[FixtureLifeCycle(LifeCycle.SingleInstance)]
public class NullSelMessageLoggerTests
{
    [Test]
    public void Log()
    {
        NullSqlMessageLogger.Instance.Log("any", 42, 1337, 11, "any");
    }
}
