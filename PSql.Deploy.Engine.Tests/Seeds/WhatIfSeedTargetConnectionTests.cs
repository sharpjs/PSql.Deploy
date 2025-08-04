// Copyright Subatomix Research Inc.
// SPDX-License-Identifier: MIT

namespace PSql.Deploy.Seeds;

[TestFixture]
public class WhatIfSeedTargetConnectionTests : TestHarnessBase
{
    private readonly WhatIfSeedTargetConnection  _outer;
    private readonly Mock<ISeedTargetConnection> _inner;
    private readonly Mock<ISqlMessageLogger>     _logger;
    private readonly Target                      _target;

    public WhatIfSeedTargetConnectionTests()
    {
        _inner  = Mocks.Create<ISeedTargetConnection>();
        _logger = Mocks.Create<ISqlMessageLogger>();
        _target = new("Server=.;Database=db");

        _inner.Setup(c => c.Target).Returns(_target);
        _inner.Setup(c => c.Logger).Returns(_logger.Object);

        _outer = new(_inner.Object);
    }

    [Test]
    public void Construct_NullConnection()
    {
        Should.Throw<ArgumentNullException>(() =>
        {
            _ = new WhatIfSeedTargetConnection(null!);
        });
    }

    [Test]
    public async Task PrepareAsync()
    {
        _logger.Setup(l => l.Log("", 0, 0, 0, "Would prepare connection."));

        await _outer.PrepareAsync(Guid.NewGuid(), Random.Next(), Cancellation.Token);
    }

    [Test]
    public async Task ExecuteSeedBatchAsync_NullSql()
    {
        await Should.ThrowAsync<ArgumentNullException>(() =>
        {
            return _outer.ExecuteSeedBatchAsync(null!, Cancellation.Token);
        });
    }

    [Test]
    public async Task ExecuteSeedBatchAsync_Ok()
    {
        _logger.Setup(l => l.Log("", 0, 0, 0, "Would execute batch beginning with: -- Foo"));

        await _outer.ExecuteSeedBatchAsync("\r\n-- Foo\r\n", Cancellation.Token);
    }

    [Test]
    //
    [TestCase(   "", "")]
    [TestCase(  "S", "")]
    [TestCase(  "L", "")]
    [TestCase( "SL", "")]
    [TestCase( "CL", "")]
    [TestCase("SCL", "")]
    //
    [TestCase(   "" + "a" +    "",  "a")]
    [TestCase(  "S" + "a" +   "S",  "a")]
    [TestCase(  "L" + "a" +   "Lx", "a")]
    [TestCase( "SL" + "a" +  "SLx", "a")]
    [TestCase( "CL" + "a" +  "CLx", "a")]
    [TestCase("SCL" + "a" + "SCLx", "a")]
    //
    [TestCase(   "" + "a b" +    "",  "a b")]
    [TestCase(  "S" + "a b" +   "S",  "a b")]
    [TestCase(  "L" + "a b" +   "Lx", "a b")]
    [TestCase( "SL" + "a b" +  "SLx", "a b")]
    [TestCase( "CL" + "a b" +  "CLx", "a b")]
    [TestCase("SCL" + "a b" + "SCLx", "a b")]
    public void GetInitialContent(string input, string expected)
    {
        input = TranslateGetInitialContentTestCase(input);

        WhatIfSeedTargetConnection.GetInitialContent(input).ShouldBe(expected);
    }

    private string TranslateGetInitialContentTestCase(string input)
    {
        Span<char> chars = stackalloc char[input.Length];

        for (int i = 0; i < chars.Length; i++)
        {
            chars[i] = input[i] switch
            {
                'S'   => Spaces[Random.Next(Spaces.Length)],
                'C'   => '\r',
                'L'   => '\n',
                var c => c,
            };
        }

        return new(chars);
    }

    private static readonly char[] Spaces = [' ', '\t', '\u2002']; // U+2002: Em Space
}
