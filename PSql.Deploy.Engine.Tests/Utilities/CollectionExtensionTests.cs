// Copyright Subatomix Research Inc.
// SPDX-License-Identifier: MIT

namespace PSql.Deploy.Utilities;

[TestFixture]
public class CollectionExtensionTests
{
    [Test]
    public void Array_AsReadOnlySpan()
    {
        var array = new[] { 1, 2, 3 };

        ReadOnlySpan<int> span = array.AsReadOnlySpan();
        // Shouldly does not provide extension methods for ReadOnlySpan<>, so
        // rely on a build error to tell if the method return type is wrong

        span.ToArray().ShouldBe(array);
    }

    [Test]
    public void ImmutableArray_SelectImmutable_NullSelector()
    {
        const Func<int, string>? NullSelector = null;

        Should.Throw<ArgumentNullException>(static () =>
        {
            Array.Empty<int>().AsReadOnlySpan().SelectImmutable(NullSelector!);
        });
    }

    [Test]
    public void ImmutableArray_SelectImmutable_Empty()
    {
        static long F(int x) => x;

        Array.Empty<int>().AsReadOnlySpan().SelectImmutable(F).ShouldBeEmpty();
    }

    [Test]
    public void ImmutableArray_SelectImmutable_NonEmpty()
    {
        var inArray  = new int [] { 1, 2, 3 };
        var outArray = new long[] { 1, 2, 3 };

        static long F(int x) => x;

        inArray.AsReadOnlySpan().SelectImmutable(F).ShouldBe(outArray);
    }

    [Test]
    public void ImmutableArrayBuilder_Build_Null()
    {
        Should.Throw<ArgumentNullException>(static () =>
        {
            default(ImmutableArray<int>.Builder)!.Build();
        });
    }

    [Test]
    public void ImmutableArrayBuilder_Build_Full()
    {
        var builder = ImmutableArray.CreateBuilder<int>(initialCapacity: 3);

        builder.Add(0);
        builder.Add(1);
        builder.Add(2);

        builder.Build().ShouldBe(new[] { 0, 1, 2 });
        builder.ShouldBeEmpty();
    }

    [Test]
    public void ImmutableArrayBuilder_Build_NotFull()
    {
        var builder = ImmutableArray.CreateBuilder<int>(initialCapacity: 3);

        builder.Add(0);
        builder.Add(1);
        //builder.Add(2); // skipped

        builder.Build().ShouldBe(new[] { 0, 1 });
        builder.ShouldBeEmpty();
    }
}
