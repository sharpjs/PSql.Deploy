// Copyright 2023 Subatomix Research Inc.
// SPDX-License-Identifier: ISC

namespace PSql.Deploy.Migrations;

[TestFixture]
public class MigrationStateExtensionsTests
{
    [Test]
    public void ToFixedWidthProgressString()
    {
        var states = (MigrationState[]) Enum.GetValues(typeof(MigrationState));

        var strings = Array.ConvertAll(states, x => x.ToFixedWidthProgressString());

        strings.Should().OnlyHaveUniqueItems()
            .And.NotContainNulls()
            .And.NotContain("");
    }
}
