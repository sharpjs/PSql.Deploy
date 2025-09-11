// Copyright Subatomix Research Inc.
// SPDX-License-Identifier: MIT

using System.Collections.ObjectModel;
using System.Linq.Expressions;

namespace PSql.Deploy;

[TestFixture]
public class DeploymentSessionTests : TestHarnessBase
{
    private Mock<TestDeploymentSession>? _session;

    private readonly DeploymentSessionOptions
        _options = new TestDeploymentSessionOptions();

    private Mock<TestDeploymentSession> SessionMock
        => _session ??= CreateSession();

    private DeploymentSession Session
        => SessionMock.Object;

    private IDeploymentSessionInternal SessionInternal
        => SessionMock.Object;

    private static readonly Target
        TargetA = new("Server=sql.example.com;Database=a"),
        TargetB = new("Server=sql.example.com;Database=b"),
        TargetC = new("Server=sql.example.com;Database=c");

    [Test]
    public void Construct_NullOptions()
    {
        Should.Throw<ArgumentNullException>(() =>
        {
            _ = new TestDeploymentSession(null!);
        });
    }

    [Test]
    public void MaxParallelism_Get()
    {
        _options.MaxParallelism = 3;

        Session.MaxParallelism.ShouldBe(3);
    }

    [Test]
    public void MaxParallelismPerTarget_Get()
    {
        _options.MaxParallelismPerTarget = 3;

        Session.MaxParallelismPerTarget.ShouldBe(3);
    }

    [Test]
    public void MaxErrorCount_Get()
    {
        _options.MaxErrorCount = 3;

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
    public async Task Apply_Target_Ok()
    {
        static void AssertParallelism(Target t, TargetParallelism p)
        {
            p.MaxActions.ShouldBe(4);
        }

        ExpectGetMaxParallelTargets(g => g.Targets.Single() == TargetA, result: 1);
        ExpectApplyCore(TargetA, AssertParallelism);

        Session.BeginApplying(TargetA, maxParallelism: 4);

        await Session.CompleteApplyingAsync(Cancellation.Token);
    }

    [Test]
    public async Task Apply_Target_GlobalParallelismLimits()
    {
        _options.MaxParallelism          = 3;
        _options.MaxParallelismPerTarget = 2;

        static void AssertParallelism(Target t, TargetParallelism p)
        {
            p.MaxActions.ShouldBe(2); // limited by global max per target
        }

        ExpectGetMaxParallelTargets(g => g.Targets.Single() == TargetA, result: 1);
        ExpectApplyCore(TargetA, AssertParallelism);

        Session.BeginApplying(TargetA, maxParallelism: 4);

        await Session.CompleteApplyingAsync(Cancellation.Token);
    }

    [Test]
    public async Task Apply_Group_Ok()
    {
        var group = new TargetGroup(
            [TargetA, TargetB],
            maxParallelism: 8,
            maxParallelismPerTarget: 1
        );

        static void AssertParallelism(Target t, TargetParallelism p)
        {
            p.MaxActions.ShouldBe(1);
        }

        ExpectGetMaxParallelTargets(g => g == group, result: 6);
        ExpectApplyCore(TargetA, AssertParallelism);
        ExpectApplyCore(TargetB, AssertParallelism);

        Session.BeginApplying(group);

        await Session.CompleteApplyingAsync(Cancellation.Token);
    }

    [Test]
    public async Task Apply_Group_GlobalParallelismLimits()
    {
        _options.MaxParallelism          = 3;
        _options.MaxParallelismPerTarget = 2;

        var group = new TargetGroup(
            [TargetA, TargetB],
            maxParallelism: 8,
            maxParallelismPerTarget: 4
        );

        static void AssertParallelism(Target t, TargetParallelism p)
        {
            p.MaxActions.ShouldBe(2);
        }

        ExpectGetMaxParallelTargets(g => g == group, result: 6);
        ExpectApplyCore(TargetA, AssertParallelism);
        ExpectApplyCore(TargetB, AssertParallelism);

        Session.BeginApplying(group);

        await Session.CompleteApplyingAsync(Cancellation.Token);
    }

    [Test]
    public async Task Apply_Any_Cancellation()
    {
        void SimulateCancellation(Target t, TargetParallelism p)
        {
            Session.Cancel();
            Session.CancellationToken.ThrowIfCancellationRequested();
        }

        ExpectApplyCore(TargetA, SimulateCancellation); // cancels session
        //pectApplyCore(TargetB);                       // never happens

        Session.BeginApplying(TargetA, maxParallelism: 1);
        await WaitForSessionCancellationAsync(); // because count of errors exceeded max (default 0)
        Session.BeginApplying(TargetB, maxParallelism: 1); // never happens

        var thrown = await Should.ThrowAsync<OperationCanceledException>(() =>
        {
            return Session.CompleteApplyingAsync(Cancellation.Token);
        });

        SessionInternal.CancellationToken.IsCancellationRequested.ShouldBeTrue();
    }

    [Test]
    public async Task Apply_Any_Exception_UpToMax()
    {
        _options.MaxErrorCount = 1;
        var exception  = new Exception("Oops!");

        ExpectApplyCore(TargetA, (t, p) => throw exception);    // error tolerated
        ExpectApplyCore(TargetB);                               // succeeds
        ExpectApplyCore(TargetC);                               // succeeds

        Session.BeginApplying(TargetA, maxParallelism: 1);
        await WaitForSessionToHaveErrorsAsync();
        Session.BeginApplying(TargetB, maxParallelism: 1);
        Session.BeginApplying(TargetC, maxParallelism: 1);

        var thrown = await Should.ThrowAsync<Exception>(() =>
        {
            return Session.CompleteApplyingAsync(Cancellation.Token);
        });

        thrown                     .ShouldBeSameAs(exception);
        thrown.Data[nameof(Target)].ShouldBe(TargetA.FullDisplayName);
    }

    [Test]
    public async Task Apply_Any_Exception_OverMax()
    {
        _options.MaxErrorCount = 1;
        var exceptionA = new Exception("Bam!");
        var exceptionB = new Exception("Pow!");

        ExpectApplyCore(TargetA, (t, p) => throw exceptionA);   // error tolerated
        ExpectApplyCore(TargetB, (t, p) => throw exceptionB);   // error cancels session
        //pectApplyCore(TargetC);                               // never happens

        Session.BeginApplying(TargetA, maxParallelism: 1);
        await WaitForSessionToHaveErrorsAsync();
        Session.BeginApplying(TargetB, maxParallelism: 1);
        await WaitForSessionCancellationAsync(); // because count of errors exceeded max
        Session.BeginApplying(TargetC, maxParallelism: 1);

        var thrown = await Should.ThrowAsync<AggregateException>(() =>
        {
            return Session.CompleteApplyingAsync(Cancellation.Token);
        });

        thrown.InnerExceptions.Count.ShouldBe(2);
        thrown.InnerExceptions[0]   .AssignTo(out var inner0);
        thrown.InnerExceptions[1]   .AssignTo(out var inner1);

        inner0                      .ShouldBeSameAs(exceptionA);
        inner0.Data[nameof(Target)] .ShouldBe(TargetA.FullDisplayName);

        inner1                      .ShouldBeSameAs(exceptionB);
        inner1.Data[nameof(Target)] .ShouldBe(TargetB.FullDisplayName);
    }

    [Test]
    public async Task Apply_Any_Exception_ReadOnlyData()
    {
        var data = new ReadOnlyDictionary<string, object?>(new Dictionary<string, object?>());

        var exception = new ExceptionWithData(data);

        ExpectApplyCore(TargetA, (t, p) => throw exception);

        Session.BeginApplying(TargetA, maxParallelism: 1);

        var thrown = await Should.ThrowAsync<ExceptionWithData>(() =>
        {
            return Session.CompleteApplyingAsync(Cancellation.Token);
        });

        thrown.ShouldBeSameAs(exception);
    }

    [Test]
    public async Task Apply_Any_Exception_NullData()
    {
        var exception = new ExceptionWithData(data: null);

        ExpectApplyCore(TargetA, (t, p) => throw exception);

        Session.BeginApplying(TargetA, maxParallelism: 1);

        var thrown = await Should.ThrowAsync<ExceptionWithData>(() =>
        {
            return Session.CompleteApplyingAsync(Cancellation.Token);
        });

        thrown.ShouldBeSameAs(exception);
    }

    private void ExpectGetMaxParallelTargets(
        Expression<Func<TargetGroup, bool>> predicate, int result)
    {
        SessionMock
            .Setup(s => s.PublicGetMaxParallelTargets_Public(It.Is(predicate)))
            .Returns(result)
            .Verifiable();
    }

    private void ExpectApplyCore(Target target, Action<Target, TargetParallelism>? callback = null)
    {
        static void Nop(Target t, TargetParallelism p) { }

        SessionMock
            .Setup(s => s.ApplyCoreAsync_Public(target, It.IsNotNull<TargetParallelism>()))
            .Callback(callback ?? Nop)
            .Returns(Task.CompletedTask)
            .Verifiable();
    }

    private Mock<TestDeploymentSession> CreateSession()
    {
        var session = Mocks.Create<TestDeploymentSession>(MockBehavior.Loose, _options);

        session.CallBase = true;

        return session;
    }

    private async Task WaitForSessionCancellationAsync()
    {
        var cancelled = new TaskCompletionSource();

        await using var _ = Session.CancellationToken.Register(cancelled.SetResult);

        await cancelled.Task.WaitAsync(TimeSpan.FromSeconds(10));
    }

    private async Task WaitForSessionToHaveErrorsAsync()
    {
        async Task WaitCoreAsync()
        {
            while (!Session.HasErrors)
                await Task.Delay(TimeSpan.FromMilliseconds(5));
        }

        await WaitCoreAsync().WaitAsync(TimeSpan.FromSeconds(10));
    }

    protected override void CleanUp(bool managed)
    {
        _session?.Object.Dispose();

        base.CleanUp(managed);
    }

    internal class TestDeploymentSessionOptions : DeploymentSessionOptions { }

    internal class TestDeploymentSession : DeploymentSession
    {
        public TestDeploymentSession(TestDeploymentSessionOptions options)
            : base(options) { }

        protected sealed override int GetMaxParallelTargets(TargetGroup group)
            => PublicGetMaxParallelTargets_Public(group);

        protected sealed override Task ApplyCoreAsync(Target target, TargetParallelism parallelism)
            => ApplyCoreAsync_Public(target, parallelism);

        public virtual int PublicGetMaxParallelTargets_Public(TargetGroup group)
            => 4;

        public virtual Task ApplyCoreAsync_Public(Target target, TargetParallelism parallelism)
            => Task.CompletedTask;
    }

    private class ExceptionWithData : Exception
    {
        public ExceptionWithData(IDictionary? data)
        {
            // This is a violation of Exception.Data's informal contract, but
            // the code under test checks for null as a defensive measure
            Data = data!;
        }

        public override IDictionary Data { get; }
    }
}
