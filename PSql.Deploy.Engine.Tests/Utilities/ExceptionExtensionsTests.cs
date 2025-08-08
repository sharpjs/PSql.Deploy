// Copyright Subatomix Research Inc.
// SPDX-License-Identifier: MIT

namespace PSql.Deploy;

using static ExceptionHelpers;

[TestFixture]
public class ExceptionExtensionsTests
{
    [Test]
    public void GetCompoundMessage_NullInnerException()
    {
        GetCompoundMessage("Bang!", null).ShouldBe("Bang!");
    }

    [Test]
    public void GetCompoundMessage_SimpleInnerException()
    {
        var inner = new Exception("Pow!");

        GetCompoundMessage("Bang!", inner).ShouldBe("Bang! Pow!");
    }

    [Test]
    public void GetCompoundMessage_DeepInnerException()
    {
        var inner = new Exception("Zap!");
        var outer = new Exception("Pow!", inner);

        GetCompoundMessage("Bang!", outer).ShouldBe("Bang! Zap!");
    }

    [Test]
    public void GetCompoundMessage_AggregateWithDeepInnerException()
    {
        var inner0 = new Exception("Klonk!");
        var inner1 = new AggregateException(inner0);
        var outer  = new AggregateException(inner1);

        GetCompoundMessage("Bang!", inner0).ShouldBe("Bang! Klonk!");
    }

    [Test]
    public void GetCompoundMessage_AggregateWithMultipleInnerExceptions()
    {
        var inner0 = new Exception("Thunk!");
        var inner1 = new Exception("Whap!");
        var outer  = new AggregateException(inner0, inner1);

        GetCompoundMessage("Bang!", outer).ShouldBe("Bang! Thunk! Whap!");
    }
}
