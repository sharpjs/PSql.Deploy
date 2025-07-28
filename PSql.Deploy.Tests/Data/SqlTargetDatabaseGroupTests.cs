// Copyright Subatomix Research Inc.
// SPDX-License-Identifier: MIT

namespace PSql.Deploy;

[TestFixture]
[TestFixtureSource(nameof(Cases))]
[FixtureLifeCycle(LifeCycle.SingleInstance)]
public class SqlTargetDatabaseGroupTests
{
    public static IEnumerable<object[]> Cases => [[false], [true]];

    private readonly Func<object, object> _wrap;

    public SqlTargetDatabaseGroupTests(bool wrapped)
    {
        _wrap = wrapped
            ? obj => new PSObject(obj)
            : obj => obj;
    }

    [Test]
    public void Construct_WithValues()
    {
        var targets = new[]
        {
            new SqlTargetDatabase("Server = localhost; Database = A"),
            new SqlTargetDatabase("Server = localhost; Database = B")
        };

        var group = new SqlTargetDatabaseGroup(
            targets,
            name:                    "My Group",
            maxParallelism:          4,
            maxParallelismPerTarget: 2
        );

        group.Targets.Count              .ShouldBe(2);
        group.Targets[0].ConnectionString.ShouldBe("Server = localhost; Database = A");
        group.Targets[1].ConnectionString.ShouldBe("Server = localhost; Database = B");
        group.Name                       .ShouldBe("My Group");
        group.MaxParallelism             .ShouldBe(4);
        group.MaxParallelismPerDatabase  .ShouldBe(2);
    }

    [Test]
    public void Construct_WithValues_Defaults()
    {
        var targets = new[]
        {
            new SqlTargetDatabase("Server = localhost; Database = A"),
            new SqlTargetDatabase("Server = localhost; Database = B")
        };

        var group = new SqlTargetDatabaseGroup(targets);

        group.Targets.Count              .ShouldBe(2);
        group.Targets[0].ConnectionString.ShouldBe("Server = localhost; Database = A");
        group.Targets[1].ConnectionString.ShouldBe("Server = localhost; Database = B");
        group.Name                       .ShouldBeNull();
        group.MaxParallelism             .ShouldBe(Environment.ProcessorCount);
        group.MaxParallelismPerDatabase  .ShouldBe(Environment.ProcessorCount)  ;
    }

    [Test]
    public void Construct_WithValues_NullTargets()
    {
        Should.Throw<ArgumentNullException>(() => new SqlTargetDatabaseGroup(targets: null!));
    }

    [Test]
    public void Construct_FromNull()
    {
        Should.Throw<ArgumentNullException>(() => new SqlTargetDatabaseGroup(obj: null!));
    }

    [Test]
    public void Construct_FromSameType()
    {
        var source = new SqlTargetDatabaseGroup(
            [
                new("Server = localhost; Database = A"),
                new("Server = localhost; Database = B")
            ],
            "My Group",
            maxParallelism:          4,
            maxParallelismPerTarget: 2
        );

        var actual = MakeSqlTargetDatabaseGroup(source);

        actual.ShouldBeEquivalentTo(source);
    }

    [Test]
    public void Construct_FromReadOnlyList()
    {
        var targets = new[]
        {
            new SqlTargetDatabase("Server = localhost; Database = A"),
            new SqlTargetDatabase("Server = localhost; Database = B")
        };

        var actual = MakeSqlTargetDatabaseGroup(targets);

        actual.Targets.Count              .ShouldBe(2);
        actual.Targets[0].ConnectionString.ShouldBe("Server = localhost; Database = A");
        actual.Targets[1].ConnectionString.ShouldBe("Server = localhost; Database = B");
        actual.Name                       .ShouldBeNull();
        actual.MaxParallelism             .ShouldBe(Environment.ProcessorCount);
        actual.MaxParallelismPerDatabase  .ShouldBe(Environment.ProcessorCount);
        // TODO: MaxParallelismPerTarget
    }

    [Test]
    public void Construct_FromEnumerable()
    {
        var targets = new ArrayList
        {
            "Server = localhost; Database = A",
            "Server = localhost; Database = B"
        };

        var actual = MakeSqlTargetDatabaseGroup(targets);

        actual.Targets.Count              .ShouldBe(2);
        actual.Targets[0].ConnectionString.ShouldBe("Server = localhost; Database = A");
        actual.Targets[1].ConnectionString.ShouldBe("Server = localhost; Database = B");
        actual.Name                       .ShouldBeNull();
        actual.MaxParallelism             .ShouldBe(Environment.ProcessorCount);
        actual.MaxParallelismPerDatabase  .ShouldBe(Environment.ProcessorCount);
        // TODO: MaxParallelismPerTarget
    }

    [Test]
    public void Construct_FromConvertible()
    {
        var actual = MakeSqlTargetDatabaseGroup("Server = localhost; Database = A");

        actual.Targets.Count              .ShouldBe(1);
        actual.Targets[0].ConnectionString.ShouldBe("Server = localhost; Database = A");
        actual.Name                       .ShouldBeNull();
        actual.MaxParallelism             .ShouldBe(Environment.ProcessorCount);
        actual.MaxParallelismPerDatabase  .ShouldBe(Environment.ProcessorCount);
        // TODO: MaxParallelismPerTarget
    }

    [Test]
    public void Construct_FromConvertible_DoesNotConformToSqlContextApi()
    {
        var obj = new TestGetConnectionStringMissingSqlContext();

        Should
            .Throw<ArgumentException>(() => MakeSqlTargetDatabaseGroup(obj))
            .Message.ShouldBe("The object does not conform to the expected API surface of PSql.SqlContext.");
    }

    [Test]
    public void Construct_FromOther()
    {
        Should
            .Throw<ArgumentException>(() => MakeSqlTargetDatabaseGroup(new object()))
            .Message.ShouldContain("Unsupported conversion.");
    }

    [Test]
    public void ToString_Unnamed_Empty()
    {
        var group = new SqlTargetDatabaseGroup(
            []
        );

        group.ToString().ShouldBe("empty");
    }

    [Test]
    public void ToString_Unnamed_One()
    {
        var group = new SqlTargetDatabaseGroup(
            [new("Server = foo; Database = bar")]
        );

        group.ToString().ShouldBe("foo.bar");
    }

    [Test]
    public void ToString_Unnamed_Many()
    {
        var group = new SqlTargetDatabaseGroup(
            [new("Server = foo; Database = bar"), new("Server = baz; Database = quux")]
        );

        group.ToString().ShouldBe("foo.bar +1");
    }

    [Test]
    public void ToString_Named_Empty()
    {
        var group = new SqlTargetDatabaseGroup(
            [],
            name: "Test"
        );

        group.ToString().ShouldBe("Test (empty)");
    }

    [Test]
    public void ToString_Named_One()
    {
        var group = new SqlTargetDatabaseGroup(
            [new("Server = foo; Database = bar")],
            name: "Test"
        );

        group.ToString().ShouldBe("Test (foo.bar)");
    }

    [Test]
    public void ToString_Named_Many()
    {
        var group = new SqlTargetDatabaseGroup(
            [new("Server = foo; Database = bar"), new("Server = baz; Database = quux")],
            name: "Test"
        );

        group.ToString().ShouldBe("Test (foo.bar +1)");
    }

    private SqlTargetDatabaseGroup MakeSqlTargetDatabaseGroup(object obj)
        => new SqlTargetDatabaseGroup(_wrap(obj));
}
