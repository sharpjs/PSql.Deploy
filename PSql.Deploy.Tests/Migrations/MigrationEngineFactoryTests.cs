// Copyright 2023 Subatomix Research Inc.
// SPDX-License-Identifier: ISC

namespace PSql.Deploy.Migrations;

[TestFixture]
public class MigrationEngineFactoryTests
{
    [Test]
    public void Create()
    {
        MigrationEngineFactory
            .Create(Mock.Of<IConsole>(), "any", default)
            .Should().BeOfType<MigrationEngine>();
    }
}
