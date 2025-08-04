// Copyright Subatomix Research Inc.
// SPDX-License-Identifier: MIT

namespace PSql.Deploy;

[TestFixture]
[FixtureLifeCycle(LifeCycle.SingleInstance)]
public class FormattingExtensionsTests
{
    [Test]
    // Because PSql.Deploy.Engine is loaded dynamically, cannot use its
    // types in the test method signature.  Use names instead.
    //               vvvvvvvvvvvvvvvvvvvvvvvvvvvvvv 
    [TestCase(nameof(E.TargetDisposition.Successful), null           )]
    [TestCase(nameof(E.TargetDisposition.Incomplete), " [INCOMPLETE]")]
    [TestCase(nameof(E.TargetDisposition.Failed),     " [EXCEPTION]" )]
    public void ToMarker(string valueName, string? expected)
    {
        var disposition = Enum.Parse<E.TargetDisposition>(valueName);

        disposition.ToMarker().ShouldBe(expected);
    }
}
