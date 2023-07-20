// Copyright 2023 Subatomix Research Inc.
// SPDX-License-Identifier: ISC

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
