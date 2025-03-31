// Copyright Subatomix Research Inc.
// SPDX-License-Identifier: MIT

namespace PSql.Deploy.Migrations;

[TestFixture]
public class MigrationDiagnosticTests
{
    [Test]
    public void Construct_NullMessage()
    {
        Should.Throw<ArgumentNullException>(static () =>
        {
            new MigrationDiagnostic(isError: true, message: null!);
        });
    }
}
