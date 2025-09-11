// Copyright Subatomix Research Inc.
// SPDX-License-Identifier: MIT

namespace PSql.Deploy;

[TestFixture]
public class DeploymentSessionOptionsTests
{
    [Test]
    public void MaxParallelism_Get_Default()
    {
        new TestDeploymentSessionOptions().MaxParallelism.ShouldBe(int.MaxValue);
    }

    [Test]
    public void MaxParallelism_Set_OutOfRange()
    {
        Should.Throw<ArgumentOutOfRangeException>(() =>
        {
            new TestDeploymentSessionOptions().MaxParallelism = 0;
        });
    }

    [Test]
    public void MaxParallelismPerTarget_Get_Default()
    {
        new TestDeploymentSessionOptions().MaxParallelismPerTarget.ShouldBe(int.MaxValue);
    }

    [Test]
    public void MaxParallelismPerTarget_Set_OutOfRange()
    {
        Should.Throw<ArgumentOutOfRangeException>(() =>
        {
            new TestDeploymentSessionOptions().MaxParallelismPerTarget = 0;
        });
    }

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
