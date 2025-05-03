// Copyright Subatomix Research Inc.
// SPDX-License-Identifier: MIT

namespace PSql.Deploy;

[TestFixture]
public class CoerceTests
{
    [Test]
    public void ToTargetSetRequired_Null()
    {
        Should.Throw<InvalidCastException>(() => Coerce.ToTargetSetRequired(null));
    }

    [Test]
    public void ToTargetSetRequired_NotNull()
    {
        var set = new TargetSet([]);

        Coerce.ToTargetSet(set).ShouldBeSameAs(set);
    }

    [Test]
    public void ToTargetSet_Null()
    {
        Coerce.ToTargetSet(null).ShouldBeNull();
    }

    [Test]
    public void ToTargetSet_TargetSet()
    {
        var set = new TargetSet([]);

        Coerce.ToTargetSet(set).ShouldBeSameAs(set);
    }

    [Test]
    public void ToTargetSet_TargetSetPso()
    {
        var set = new TargetSet([]).InPSObject();

        Coerce.ToTargetSet(set).ShouldBeSameAs(set.BaseObject);
    }
}
