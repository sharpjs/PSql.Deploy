// Copyright Subatomix Research Inc.
// SPDX-License-Identifier: MIT

namespace PSql.Deploy.Migrations;

[TestFixture]
public class MigrationReferenceTests
{
    [Test]
    public void Construct_NullName()
    {
        Should.Throw<ArgumentNullException>(static () =>
        {
            new MigrationReference(name: null!);
        });
    }
}
