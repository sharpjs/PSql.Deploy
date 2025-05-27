// Copyright Subatomix Research Inc.
// SPDX-License-Identifier: MIT

namespace PSql.Deploy;

[TestFixture]
public class ImmediateDispatcherTests
{
    [Test]
    public void Post()
    {
        var invoked = false;

        ImmediateDispatcher.Instance.Post(() => invoked = true);

        invoked.ShouldBeTrue();
    }
}
