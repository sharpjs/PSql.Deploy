// Copyright 2023 Subatomix Research Inc.
// SPDX-License-Identifier: ISC

namespace PSql.Deploy.Migrations;

[TestFixture]
public class MigrationReferenceTests
{
    [Test]
    public void Construct_NullName()
    {
        Invoking(() => new MigrationReference(null!))
            .Should().Throw<ArgumentNullException>();
    }
}
