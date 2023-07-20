// Copyright 2023 Subatomix Research Inc.
// SPDX-License-Identifier: ISC

namespace PSql.Deploy.Migrations;

using static Math;

[TestFixture]
public class MigrationComparerTests
{
    [Test]
    //
    [TestCase(null,     null,      0)]
    [TestCase(null,     "_Begin", -1)]
    [TestCase(null,     "x",      -1)]
    [TestCase(null,     "_End",   -1)]
    // 
    [TestCase("_Begin", null,     +1)]
    [TestCase("_Begin", "_Begin",  0)]
    [TestCase("_Begin", "x",      -1)]
    [TestCase("_Begin", "_End",   -1)]
    //
    [TestCase("x",       null,     +1)]
    [TestCase("x",       "_Begin", +1)]
    [TestCase("x",       "a",      +1)]
    [TestCase("x",       "x",       0)]
    [TestCase("x",       "z",      -1)]
    [TestCase("x",       "_End",   -1)]
    //
    [TestCase("_End",    null,     +1)]
    [TestCase("_End",    "_Begin", +1)]
    [TestCase("_End",    "x",      +1)]
    [TestCase("_End",    "_End",    0)]
    public void Compare(string? nameX, string? nameY, int expected)
    {
        var x = nameX is null ? null : new Migration(nameX);
        var y = nameY is null ? null : new Migration(nameY);

        Sign(MigrationComparer.Instance.Compare(x, y)).Should().Be(expected);
    }

    [TestCase("_Begin", -1)]
    [TestCase("x",       0)]
    [TestCase("_End",   +1)]
    public void GetRank(string name, int expected)
    {
        MigrationComparer.GetRank(name).Should().Be(expected);
    }
}
