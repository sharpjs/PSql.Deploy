// Copyright Subatomix Research Inc.
// SPDX-License-Identifier: MIT

namespace PSql.Deploy.Commands;

using static ScriptExecutor;

[TestFixture]
internal class NewSqlTargetDatabaseGroupCommandTests
{
    [Test]
    public void Invoke()
    {
        var (output, exception) = Execute(
            """
            New-SqlTargetDatabaseGroup 'Server=.;Database=a', (New-SqlContext -DatabaseName b)
            """
        );

        var group = ShouldBeGroup(output);

        group.Name                     .ShouldBeNull();
        group.Targets                  .ShouldNotBeNull().AssignTo(out var targets);
        group.Targets.Count            .ShouldBe(2);
        group.MaxParallelism           .ShouldBe(Environment.ProcessorCount);
        group.MaxParallelismPerDatabase.ShouldBe(Environment.ProcessorCount);

        targets[0].ShouldNotBeNull();
        targets[0].ConnectionString.ShouldBe("Server=.;Database=a");

        targets[1].ShouldNotBeNull();
        targets[1].ConnectionString.ShouldContain("Data Source=.");
        targets[1].ConnectionString.ShouldContain("Initial Catalog=b");
    }

    [Test]
    public void Invoke_Pipeline()
    {
        var (output, exception) = Execute(
            """
            $(
                'Server=.;Database=a'
                (New-SqlContext -DatabaseName b)
                [PSql.Deploy.SqlTargetDatabase]::new('Server=.;Database=c')
            ) `
            | New-SqlTargetDatabaseGroup
            """
        );

        var group = ShouldBeGroup(output);

        group.Name                     .ShouldBeNull();
        group.Targets                  .ShouldNotBeNull().AssignTo(out var targets);
        group.Targets.Count            .ShouldBe(3);
        group.MaxParallelism           .ShouldBe(Environment.ProcessorCount);
        group.MaxParallelismPerDatabase.ShouldBe(Environment.ProcessorCount);

        targets[0].ShouldNotBeNull();
        targets[0].ConnectionString.ShouldBe("Server=.;Database=a");

        targets[1].ShouldNotBeNull();
        targets[1].ConnectionString.ShouldContain("Data Source=.");
        targets[1].ConnectionString.ShouldContain("Initial Catalog=b");

        targets[2].ShouldNotBeNull();
        targets[2].ConnectionString.ShouldBe("Server=.;Database=c");
    }

    [Test]
    public void Invoke_WithName()
    {
        var (output, exception) = Execute(
            """
            New-SqlTargetDatabaseGroup 'Server=.;Database=a' -Name GroupA
            """
        );

        var group = ShouldBeGroup(output);

        group.Name                     .ShouldBe("GroupA");
        group.Targets                  .ShouldNotBeNull().AssignTo(out var targets);
        group.Targets.Count            .ShouldBe(1);
        group.MaxParallelism           .ShouldBe(Environment.ProcessorCount);
        group.MaxParallelismPerDatabase.ShouldBe(Environment.ProcessorCount);

        targets[0].ShouldNotBeNull();
        targets[0].ConnectionString.ShouldBe("Server=.;Database=a");
    }

    [Test]
    public void Invoke_WithMaxParallelism()
    {
        var (output, exception) = Execute(
            """
            New-SqlTargetDatabaseGroup 'Server=.;Database=a' -MaxParallelism 2
            """
        );

        var group = ShouldBeGroup(output);

        group.Name                     .ShouldBeNull();
        group.Targets                  .ShouldNotBeNull().AssignTo(out var targets);
        group.Targets.Count            .ShouldBe(1);
        group.MaxParallelism           .ShouldBe(2);
        group.MaxParallelismPerDatabase.ShouldBe(Environment.ProcessorCount);

        targets[0].ShouldNotBeNull();
        targets[0].ConnectionString.ShouldBe("Server=.;Database=a");
    }

    [Test]
    public void Invoke_WithMaxParallelismPerDatabase()
    {
        var (output, exception) = Execute(
            """
            New-SqlTargetDatabaseGroup 'Server=.;Database=a' -MaxParallelismPerDatabase 2
            """
        );

        var group = ShouldBeGroup(output);

        group.Name                     .ShouldBeNull();
        group.Targets                  .ShouldNotBeNull().AssignTo(out var targets);
        group.Targets.Count            .ShouldBe(1);
        group.MaxParallelism           .ShouldBe(Environment.ProcessorCount);
        group.MaxParallelismPerDatabase.ShouldBe(2);

        targets[0].ShouldNotBeNull();
        targets[0].ConnectionString.ShouldBe("Server=.;Database=a");
    }

    public SqlTargetDatabaseGroup ShouldBeGroup(IReadOnlyCollection<PSObject?> output)
    {
        return output
            .ShouldHaveSingleItem()
            .ShouldNotBeNull()
            .BaseObject.ShouldBeOfType<SqlTargetDatabaseGroup>();
    }
}
