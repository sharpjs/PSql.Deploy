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
    public void Parallelism_Get()
    {
        new SqlContextParallelSet().Parallelism.Should().Be(Environment.ProcessorCount);
    }

    [Test]
    public void Parallelism_Set()
    {
        new SqlContextParallelSet() { Parallelism = 42 }
            .Parallelism.Should().Be(42);
    }

    [Test]
    public void Parallelism_Set_OutOfRange()
    {
        new SqlContextParallelSet()
            .Invoking(s => s.Parallelism = 0)
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
