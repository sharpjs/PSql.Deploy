// Copyright 2024 Subatomix Research Inc.
// SPDX-License-Identifier: ISC

namespace PSql.Deploy.Seeding;

[TestFixture]
public class SeedApplicatorTests
{
    [Test]
    public void Construct_NullSession()
    {
        Invoking(() => new SeedApplicator(null!, MakeSeed(), Target))
            .Should().Throw<ArgumentNullException>();
    }

    [Test]
    public void Construct_NullSeed()
    {
        var session = Mock.Of<ISeedSession>();

        Invoking(() => new SeedApplicator(session, null!, Target))
            .Should().Throw<ArgumentNullException>();
    }

    [Test]
    public void Construct_NullTarget()
    {
        var session = Mock.Of<ISeedSession>();

        Invoking(() => new SeedApplicator(session, MakeSeed(), null!))
            .Should().Throw<ArgumentNullException>();
    }

    [Test]
    public void SeedName_Get()
    {
        var seed = MakeSeed("Foo");

        using var h = new TestHarness(seed: seed);

        h.Applicator.SeedName.Should().Be(seed.Seed.Name);
    }

    [Test]
    public void ServerName_Get()
    {
        using var h = new TestHarness();

        h.Applicator.ServerName.Should().Be(Target.ServerDisplayName);
    }

    [Test]
    public void DatabaseName_Get()
    {
        using var h = new TestHarness();

        h.Applicator.DatabaseName.Should().Be(Target.DatabaseDisplayName);
    }

    [Test]
    public void Context_Get()
    {
        using var h = new TestHarness();

        h.Applicator.Context.Should().BeSameAs(Target.Context);
    }

    [Test]
    public void SqlStrategy_Get()
    {
        using var h = new TestHarness();

        h.Applicator.SqlStrategy.Should().BeSameAs(h.SqlStrategy.Object);
    }

    [Test]
    public async Task ApplyAsync_Empty()
    {
        using var h = new TestHarness();

        h.SetUpConnectToDatabase();

        h.Console
            .Setup(c => c.ReportStarting())
            .Verifiable();

        h.Console
            .Setup(c => c.ReportApplied(0, It.IsAny<TimeSpan>(), TargetDisposition.Successful))
            .Verifiable();

        await h.Applicator.ApplyAsync();
    }

    private static readonly SqlContextWork Target = new
    (
        new SqlContext()
        {
            ServerName   = "db.example.com",
            DatabaseName = "test"
        }
        .Freeze()
    );

    private static LoadedSeed MakeSeed(string name = "A", params SeedModule[] modules)
    {
        return new(new Seed(name, @"C:\" + name), MakeArray(modules));
    }

    private static SeedModule MakeModule(
        string    name     = "A",
        string[]? batches  = null,
        string[]? provides = null,
        string[]? requires = null)
    {
        return new(name, MakeArray(batches), MakeArray(provides), MakeArray(requires));
    }

    private static ImmutableArray<T> MakeArray<T>(T[]? items)
    {
        return items is not null
            ? ImmutableArray.Create(items)
            : ImmutableArray<T>.Empty;
    }

    private class TestHarness : TestHarnessBase
    {
        public TestHarness(bool whatIf = false, LoadedSeed? seed = null)
        {
            Session       = Mocks.Create<ISeedSession>();
            Console       = Mocks.Create<ISeedConsole>();
            SqlStrategy   = Mocks.Create<ISqlStrategy>();
            SqlConnection = Mocks.Create<ISqlConnection>();
            SqlCommand    = Mocks.Create<ISqlCommand>();
            SqlSequence   = new();
            Log           = new();

            SetUpSessionForConstructor(whatIf, seed ??= MakeSeed());

            Applicator = new(Session.Object, seed, Target)
            {
                SqlStrategy = SqlStrategy.Object
            };

            SetUpSessionForApplyAsync(whatIf);
        }

        private void SetUpSessionForConstructor(bool whatIf, LoadedSeed seed)
        {
            Session
                .Setup(s => s.IsWhatIfMode)
                .Returns(whatIf);

            Session
                .Setup(s => s.CreateLog(seed.Seed, Target))
                .Returns(Log)
                .Verifiable();
        }

        private void SetUpSessionForApplyAsync(bool whatIf)
        {
            Session.Verify();
            Session.Reset();

            Session
                .Setup(s => s.IsWhatIfMode)
                .Returns(whatIf);

            Session
                .Setup(s => s.Console)
                .Returns(Console.Object);

            Session
                .Setup(c => c.MaxParallelism)
                .Returns(1);

            Session
                .Setup(c => c.HasErrors)
                .Returns(false);

            Session
                .Setup(c => c.CancellationToken)
                .Returns(Cancellation.Token);
        }

        public void SetUpConnectToDatabase()
        {
            SqlStrategy
                .Setup(s => s.ConnectAsync(
                    Target.Context,
                    It.IsNotNull<ISqlMessageLogger>(),
                    Cancellation.Token
                ))
                .ReturnsAsync(SqlConnection.Object);

            SqlConnection
                .Setup(c => c.CreateCommand())
                .Returns(SqlCommand.Object);

            SqlConnection
                .Setup(c => c.Dispose())
                .Verifiable();

            SqlCommand
                .SetupAllProperties();

            SqlCommand
                .Setup(c => c.Dispose())
                .Verifiable();

            SetUpExecuteSqlCommand(s => s.Should().StartWith("-- PrepareAsync"));
        }

        public void SetUpExecuteSqlCommand(Action<string> assertion)
        {
            void AssertExpected(ISqlCommand command, CancellationToken _)
                => assertion(command.CommandText);

            SqlStrategy
                .InSequence(SqlSequence)
                .Setup(s => s.ExecuteNonQueryAsync(SqlCommand.Object, Cancellation.Token))
                .Callback(AssertExpected)
                .Returns(Task.CompletedTask)
                .Verifiable();
        }

        public SeedApplicator       Applicator    { get; }
        public Mock<ISeedSession>   Session       { get; }
        public Mock<ISeedConsole>   Console       { get; }
        public Mock<ISqlStrategy>   SqlStrategy   { get; }
        public Mock<ISqlConnection> SqlConnection { get; }
        public Mock<ISqlCommand>    SqlCommand    { get; }
        public MockSequence         SqlSequence   { get; }
        public StringWriter         Log           { get; }

        protected override void CleanUp(bool managed)
        {
            Log.Dispose();

            base.CleanUp(managed);
        }
    }
}
