// Copyright 2023 Subatomix Research Inc.
// SPDX-License-Identifier: ISC

namespace PSql.Deploy;

[TestFixture]
public class ColumnTests
{
    [Test]
    public void Default()
    {
        new Column().Width.Should().Be(0);
    }

    [Test]
    public void Construct_String()
    {
        new Column("FOO").Width.Should().Be(3);
    }

    [Test]
    public void Construct_Width()
    {
        new Column(42).Width.Should().Be(42);
    }

    [Test]
    public void Fit_String()
    {
        var column = new Column();

        column.Fit("FOO");
        column.Fit("HELLO");
        column.Fit("BAR");

        column.Width.Should().Be(5);
    }

    [Test]
    public void Fit_Width()
    {
        var column = new Column();

        column.Fit( 3);
        column.Fit(42);
        column.Fit( 3);

        column.Width.Should().Be(42);
    }

    [Test]
    public void GetPadding()
    {
        new Column("NAME").GetPadding("a").Should().Be("   ");
    }
}
