// Copyright Subatomix Research Inc.
// SPDX-License-Identifier: MIT

namespace PSql.Deploy.Engine.Tests;

[TestFixture]
public class TargetTests
{
    [Test]
    public void Construct_NullConnectionString()
    {
        Should.Throw<ArgumentNullException>(() =>
        {
            new Target(connectionString: null!);
        });
    }

    // TODO: More
}
