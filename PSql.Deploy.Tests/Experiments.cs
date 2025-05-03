// Copyright Subatomix Research Inc.
// SPDX-License-Identifier: MIT

namespace PSql.Deploy;

[TestFixture]
public class Experiments
{
    [Test]
    public void ArrayCasting()
    {
        var objectArray = new object[] { "a", "b" };
        var stringArray = new string[] { "a", "b" };

        var upcastArray   = (object[]) stringArray;
        //var downcastArray = (string[]) objectArray;

        upcastArray.Should().BeSameAs(stringArray);

        (stringArray is object[]).Should().BeTrue();
        (objectArray is string[]).Should().BeFalse();
    }
}
