// Copyright 2023 Subatomix Research Inc.
// SPDX-License-Identifier: ISC

namespace PSql.Deploy;

[TestFixture]
public class SqlContextParallelSetTests
{
    [Test]
    public void Name_Get()
    {
        new SqlContextParallelSet().Name.Should().BeNull();
    }

    [Test]
    public void Name_Set()
    {
        new SqlContextParallelSet() { Name = "bob" }
            .Name.Should().Be("bob");
    }

    [Test]
    public void MaxParallelism_Get()
    {
        new SqlContextParallelSet()
            .MaxParallelism.Should().Be(Environment.ProcessorCount);
    }

    [Test]
    public void MaxParallelism_Set()
    {
        new SqlContextParallelSet() { MaxParallelism = 42 }
            .MaxParallelism.Should().Be(42);
    }

    [Test]
    public void MaxParallelism_Set_OutOfRange()
    {
        new SqlContextParallelSet()
            .Invoking(s => s.MaxParallelism = 0)
            .Should().Throw<ArgumentOutOfRangeException>();
    }

    [Test]
    public void MaxParallelismPerDatabase_Get()
    {
        new SqlContextParallelSet()
            .MaxParallelismPerDatabase.Should().Be(Environment.ProcessorCount);
    }

    [Test]
    public void MaxParallelismPerDatabase_Set()
    {
        new SqlContextParallelSet() { MaxParallelismPerDatabase = 42 }
            .MaxParallelismPerDatabase.Should().Be(42);
    }

    [Test]
    public void MaxParallelismPerDatabase_Set_OutOfRange()
    {
        new SqlContextParallelSet()
            .Invoking(s => s.MaxParallelismPerDatabase = 0)
            .Should().Throw<ArgumentOutOfRangeException>();
    }

    [Test]
    public void Contexts_Get()
    {
        new SqlContextParallelSet().Contexts.Should().BeEmpty();
    }

    [Test]
    public void Contexts_Set()
    {
        var value = new SqlContext[0];

        new SqlContextParallelSet() { Contexts = value }
            .Contexts.Should().BeSameAs(value);
    }

    [Test]
    public void Contexts_Set_OutOfRange()
    {
        new SqlContextParallelSet()
            .Invoking(s => s.Contexts = null!)
            .Should().Throw<ArgumentNullException>();
    }
}
