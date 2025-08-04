// Copyright Subatomix Research Inc.
// SPDX-License-Identifier: MIT

namespace PSql.Deploy;

[TestFixture]
public class ColumnTests
{
    [Test]
    public void Default()
    {
        new Column().Width.ShouldBe(0);
    }

    [Test]
    public void Construct_String()
    {
        new Column("FOO").Width.ShouldBe(3);
    }

    [Test]
    public void Construct_Width()
    {
        new Column(42).Width.ShouldBe(42);
    }

    [Test]
    public void Fit_String()
    {
        var column = new Column();

        column.Fit("FOO");
        column.Fit("HELLO");
        column.Fit("BAR");

        column.Width.ShouldBe(5);
    }

    [Test]
    public void Fit_Width()
    {
        var column = new Column();

        column.Fit( 3);
        column.Fit(42);
        column.Fit( 3);

        column.Width.ShouldBe(42);
    }

    [Test]
    public void GetPadding()
    {
        new Column("NAME").GetPadding("a").ShouldBe("   ");
    }
}
