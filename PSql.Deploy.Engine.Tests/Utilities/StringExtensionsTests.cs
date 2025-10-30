// Copyright Subatomix Research Inc.
// SPDX-License-Identifier: MIT

namespace PSql.Deploy;

[TestFixture]
public class StringExtensionsTests
{
    [Test]
    [TestCase(null, true )]
    [TestCase("",   true )]
    [TestCase("a",  false)]
    public void IsNullOrEmpty(string? s, bool expected)
    {
        s.IsNullOrEmpty().ShouldBe(expected);
    }

    [Test]
    [TestCase(null, null)]
    [TestCase("",   null)]
    [TestCase("a",  "a" )]
    public void NullIfEmpty(string? s, string? expected)
    {
        s.NullIfEmpty().ShouldBe(expected);
    }

    [Test]
    [TestCase("a", "a" )]
    [TestCase("'", "''")]
    public void EscapeForSqlString(string s, string expected)
    {
        s.EscapeForSqlString().ShouldBe(expected);
    }
}
