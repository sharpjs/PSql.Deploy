// Copyright Subatomix Research Inc.
// SPDX-License-Identifier: MIT

namespace PSql.Deploy;

[TestFixture]
public class EmbeddedResourceTests
{
    private object? _location;

    [Test]
    public async Task LazyLoad()
    {
        const int IterationCount = 128;

        string Load() => EmbeddedResource.LazyLoad(
            ref _location, GetType(), "Core.EmbeddedResourceTests.dat"
        );

        var results = await Task.WhenAll(
            Enumerable.Range(0, IterationCount).Select(_ => Task.Run(Load))
        );

        var first = results.First();

        foreach (var result in results)
            result.ShouldBeSameAs(first);
    }

    [Test]
    public void Load_NotFound()
    {
        Should.Throw<FileNotFoundException>(() =>
        {
            EmbeddedResource.Load(GetType(), "does not exist");
        });
    }
}
