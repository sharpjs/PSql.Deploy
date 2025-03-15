// Copyright Subatomix Research Inc.
// SPDX-License-Identifier: MIT

namespace PSql.Deploy.Migrations;

[TestFixture]
public class MigrationDiagnosticTests
{
    [Test]
    public void Construct_NullMessage()
    {
        Invoking(() => new MigrationDiagnostic(isError: true, message: null!))
            .Should().Throw<ArgumentNullException>();
    }
}
