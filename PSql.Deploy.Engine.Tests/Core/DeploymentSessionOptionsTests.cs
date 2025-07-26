// Copyright Subatomix Research Inc.
// SPDX-License-Identifier: MIT

namespace PSql.Deploy;

[TestFixture]
public class DeploymentSessionOptionsTests
{
    [Test]
    public void MaxErrorCount_Set_OutOfRange()
    {
        Should.Throw<ArgumentOutOfRangeException>(() =>
        {
            new TestDeploymentSessionOptions().MaxErrorCount = -1;
        });
    }

    private class TestDeploymentSessionOptions : DeploymentSessionOptions { }
}
