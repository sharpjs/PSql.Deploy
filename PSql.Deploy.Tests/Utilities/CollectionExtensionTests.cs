// Copyright 2023 Subatomix Research Inc.
// SPDX-License-Identifier: ISC

namespace PSql.Deploy.Utilities;

[TestFixture]
public class CollectionExtensionTests
{
    [Test]
    public void Array_AsReadOnlySpan()
    {
        var array = new[] { 1, 2, 3 };

        ReadOnlySpan<int> span = array.AsReadOnlySpan();
        // FluentAssertions does not support ReadOnlySpan<>, so rely on a build
        // error to tell if the method return type is wrong

        span.ToArray().Should().Equal(array);
    }

    [Test]
    public void ImmutableArray_SelectImmutable_NullSelector()
    {
        const Func<int, string>? NullSelector = null;

        Array.Empty<int>()
            .Invoking(a => a.AsReadOnlySpan().SelectImmutable(NullSelector!))
            .Should().Throw<ArgumentNullException>();
    }

    [Test]
    public void ImmutableArray_SelectImmutable_Empty()
    {
        static long F(int x) => x;

        Array.Empty<int>().AsReadOnlySpan().SelectImmutable(F)
            .Should().BeEmpty();
    }

    [Test]
    public void ImmutableArray_SelectImmutable_NonEmpty()
    {
        var array = new[] { 1, 2, 3 };

        static long F(int x) => x;

        array.AsReadOnlySpan().SelectImmutable(F)
            .Should().BeEquivalentTo(array);
    }

    [Test]
    public void ImmutableArrayBuilder_Build_Null()
    {
        Invoking(() => default(ImmutableArray<int>.Builder)!.Build())
            .Should().Throw<ArgumentNullException>();
    }

    [Test]
    public void ImmutableArrayBuilder_Build_Full()
    {
        var builder = ImmutableArray.CreateBuilder<int>(initialCapacity: 3);

        builder.Add(0);
        builder.Add(1);
        builder.Add(2);

        builder.Build().Should().Equal(0, 1, 2);
        builder.Should().BeEmpty();
    }

    [Test]
    public void ImmutableArrayBuilder_Build_NotFull()
    {
        var builder = ImmutableArray.CreateBuilder<int>(initialCapacity: 3);

        builder.Add(0);
        builder.Add(1);
        //builder.Add(2); // skipped

        builder.Build().Should().Equal(0, 1);
        builder.Should().BeEmpty();
    }
}
