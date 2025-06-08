// Copyright Subatomix Research Inc.
// SPDX-License-Identifier: MIT


using System.Threading.Tasks;
using Moq.Protected;

namespace PSql.Deploy;

[TestFixture]
public class DeploymentSessionTests : TestHarnessBase
{
    private Mock<DeploymentSession>? _session;
    private int                      _maxErrorCount;

    private Mock<DeploymentSession> SessionMock
        => _session ??= CreateSession();

    private DeploymentSession Session
        => SessionMock.Object;

    private IDeploymentSessionInternal SessionInternal
        => SessionMock.Object;

    private static readonly Target
        TargetA = new Target("Server=sql.example.com;Database=a"),
        TargetB = new Target("Server=sql.example.com;Database=b"),
        TargetC = new Target("Server=sql.example.com;Database=c");

    [Test]
    public void Construct_NegativeMaxErrorCount()
    {
        Should.Throw<ArgumentOutOfRangeException>(() =>
        {
            _ = new TestDeploymentSession(maxErrorCount: -1);
        });
    }

    [Test]
    public void MaxErrorCount_Get()
    {
        _maxErrorCount = 3;

        Session.MaxErrorCount.ShouldBe(3);
    }

    [Test]
    public void HasErrors_Initial()
    {
        Session.HasErrors.ShouldBeFalse();
    }

    [Test]
    public void BeginApplying_Target_NullTarget()
    {
        Should.Throw<ArgumentNullException>(() =>
        {
            Session.BeginApplying(target: null!);
        });
    }

    [Test]
    public void BeginApplying_Target_NegativeParallelism()
    {
        Should.Throw<ArgumentOutOfRangeException>(() =>
        {
            Session.BeginApplying(TargetA, maxParallelism: -1);
        });
    }

    [Test]
    public void BeginApplying_Group_NullTarget()
    {
        Should.Throw<ArgumentNullException>(() =>
        {
            Session.BeginApplying(group: null!);
        });
    }

    [Test]
    public async Task Apply_Target_Exception()
    {
        SessionMock
            .Protected()
            .Setup<Task>("ApplyCoreAsync", ItExpr.IsAny<Target>(), ItExpr.IsAny<int>())
            .Returns(Task.CompletedTask)
            .Verifiable();

        Session.BeginApplying(TargetA);

        await Session.CompleteApplyingAsync(Cancellation.Token);
    }

    private Mock<DeploymentSession> CreateSession()
    {
        var session = Mocks.Create<DeploymentSession>(MockBehavior.Loose, _maxErrorCount);

        session.CallBase = true;

        return session;
    }

    protected override void CleanUp(bool managed)
    {
        _session?.Object.Dispose();

        base.CleanUp(managed);
    }

    private class TestDeploymentSession : DeploymentSession
    {
        public TestDeploymentSession(int maxErrorCount)
            : base(maxErrorCount) { }

        public override bool IsWhatIfMode
            => false;

        protected override Task ApplyCoreAsync(Target target, int maxParallelism)
            => Task.CompletedTask;
    }
}
