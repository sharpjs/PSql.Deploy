// Copyright Subatomix Research Inc.
// SPDX-License-Identifier: MIT

namespace PSql.Deploy.Migrations;

[TestFixture]
public class MigrationStateExtensionsTests
{
    [Test]
    public void ToFixedWidthProgressString()
    {
        var states = (MigrationState[]) Enum.GetValues(typeof(MigrationState));

        var strings = Array.ConvertAll(states, x => x.ToFixedWidthString());

        strings.Should().OnlyHaveUniqueItems()
            .And.NotContainNulls()
            .And.NotContain("");
    }
}
