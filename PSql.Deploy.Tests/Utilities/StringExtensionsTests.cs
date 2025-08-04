// Copyright Subatomix Research Inc.
// SPDX-License-Identifier: MIT

namespace PSql.Deploy;

[TestFixture]
public class StringExtensionsTests
{
    [Test]
    [TestCase(null,     null    )]
    [TestCase("",       ""      )]
    [TestCase("foo",    "foo"   )]
    [TestCase("f/o//o", "f_o__o")]
    public void SanitizeFileName(string? input, string? expected)
    {
        input.SanitizeFileName().ShouldBe(expected);
    }
}
