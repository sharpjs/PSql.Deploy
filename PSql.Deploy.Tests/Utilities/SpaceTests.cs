// Copyright 2023 Subatomix Research Inc.
// SPDX-License-Identifier: ISC

namespace PSql.Deploy;

[TestFixture]
public class SpaceTests
{
    [Test]
    [TestCase("",      0,     "")]
    [TestCase("",      4, "    ")]
    [TestCase("abc",   4,    " ")]
    [TestCase("abcd",  4,     "")]
    [TestCase("abcde", 4,     "")]
    public void Pad(string s, int width, string expected)
    {
        var a = Space.Pad(s, width);
        var b = Space.Pad(s, width);

        a.Should().Be(expected).And.BeSameAs(b); 
    }

    [Test]
    [TestCase(-1, "")]
    [TestCase( 0, "")]
    [TestCase( 1, " ")]
    [TestCase(10, "          ")]
    public void Get(int n, string expected)
    {
        var a = Space.Get(n);
        var b = Space.Get(n);

        a.Should().Be(expected).And.BeSameAs(b); 
    }

    [Test]
    [TestCase(int.MinValue, "",     "")]
    [TestCase(-1,           "",     "")]
    [TestCase( 0,           "",     "")]
    [TestCase( 1,           " ",    "")]
    [TestCase( 2,           " ",   " ")]
    [TestCase( 3,           "  ",  " ")]
    [TestCase( 4,           "  ", "  ")]
    public void GetCentering(int n, string expectedLeft, string expectedRight)
    {
        var a = Space.GetCentering(n);
        var b = Space.GetCentering(n);

        a.Should().Be((expectedLeft, expectedRight)).And.Be(b);
    }
}
