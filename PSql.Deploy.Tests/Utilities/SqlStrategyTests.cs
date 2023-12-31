// Copyright 2023 Subatomix Research Inc.
// SPDX-License-Identifier: ISC

namespace PSql.Deploy;

[TestFixture]
public class SqlStrategyTests
{
    [Test]
    public void GetInstance_Default()
    {
        SqlStrategy.GetInstance(isWhatIfMode: false)
            .Should().BeOfType<DefaultSqlStrategy>();
    }

    [Test]
    public void GetInstance_WhatIf()
    {
        SqlStrategy.GetInstance(isWhatIfMode: true)
            .Should().BeOfType<NullSqlStrategy>();
    }
}
