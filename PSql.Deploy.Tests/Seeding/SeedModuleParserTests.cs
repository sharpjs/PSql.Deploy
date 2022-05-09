/*
    Copyright 2022 Jeffrey Sharp

    Permission to use, copy, modify, and distribute this software for any
    purpose with or without fee is hereby granted, provided that the above
    copyright notice and this permission notice appear in all copies.

    THE SOFTWARE IS PROVIDED "AS IS" AND THE AUTHOR DISCLAIMS ALL WARRANTIES
    WITH REGARD TO THIS SOFTWARE INCLUDING ALL IMPLIED WARRANTIES OF
    MERCHANTABILITY AND FITNESS. IN NO EVENT SHALL THE AUTHOR BE LIABLE FOR
    ANY SPECIAL, DIRECT, INDIRECT, OR CONSEQUENTIAL DAMAGES OR ANY DAMAGES
    WHATSOEVER RESULTING FROM LOSS OF USE, DATA OR PROFITS, WHETHER IN AN
    ACTION OF CONTRACT, NEGLIGENCE OR OTHER TORTIOUS ACTION, ARISING OUT OF
    OR IN CONNECTION WITH THE USE OR PERFORMANCE OF THIS SOFTWARE.
*/

using DependencyQueue;

namespace PSql.Deploy.Seeding;

using Queue  = DependencyQueue      <IEnumerable<string>>;
using Module = DependencyQueueEntry <IEnumerable<string>>;

[TestFixture]
[TestFixtureSource(typeof(NewLineMode), nameof(NewLineMode.All))]
public class SeedModuleParserTests
{
    private readonly string _newLine;

    public SeedModuleParserTests(NewLineMode mode)
    {
        _newLine = mode.NewLineString;
    }

    [Test]
    public void Construct_NullQueue()
    {
        Invoking(() => new SeedModuleParser(null!))
            .Should().Throw<ArgumentNullException>()
            .Where(e => e.ParamName == "queue");
    }

    [Test]
    public void Complete_WithoutProcess()
    {
        var queue  = new Queue();
        var parser = new SeedModuleParser(queue);

        parser.Complete();

        Assert(queue, Module("(init)"));
    }

    [Test]
    public void Complete_Completed()
    {
        var queue  = new Queue();
        var parser = new SeedModuleParser(queue);

        parser.Complete();

        parser
            .Invoking(p => p.Complete())
            .Should().Throw<InvalidOperationException>();
    }

    [Test]
    public void Process_Completed()
    {
        var queue  = new Queue();
        var parser = new SeedModuleParser(queue);

        parser.Complete();

        parser
            .Invoking(p => p.Process("any"))
            .Should().Throw<InvalidOperationException>();
    }

    [Test]
    public void Process_NullText()
    {
        var queue  = new Queue();
        var parser = new SeedModuleParser(queue);

        parser
            .Invoking(p => p.Process(null!))
            .Should().Throw<ArgumentNullException>()
            .Where(e => e.ParamName == "text");
    }

    [Test]
    public void Process_EmptyText()
    {
        var queue  = new Queue();
        var parser = new SeedModuleParser(queue);

        parser.Process("");
        parser.Process("");
        parser.Complete();

        Assert(queue, Module("(init)"));
    }

    [Test]
    public void Process_Init()
    {
        var queue  = new Queue();
        var parser = new SeedModuleParser(queue);

        parser.Process("a");
        parser.Process("b");
        parser.Complete();

        Assert(queue, Module("(init)", "a", "b"));
    }

    [Test]
    public void Process_String()
    {
        var queue  = new Queue();
        var parser = new SeedModuleParser(queue);

        var script = Lines(
            "'A multi-line string with an escaped '' single quote.",
            "--# MODULE: Not a module! This is in a string.'",
            "'Another multi-line string.",
            "--# MODULE: Not a module! This is in an unterminated string."
        );

        parser.Process(script);
        parser.Complete();

        Assert(queue, Module("(init)", script));
    }

    [Test]
    public void Process_QuotedIdentifier()
    {
        var queue  = new Queue();
        var parser = new SeedModuleParser(queue);

        var script = Lines(
            "[A multi-line quoted identifier with an escaped ]] square bracket.",
            "--# MODULE: Not a module! This is in a quoted identifier.]",
            "[Another multi-line quoted identifier.",
            "--# MODULE: Not a module! This is in an unterminated quoted identifier."
        );

        parser.Process(script);
        parser.Complete();

        Assert(queue, Module("(init)", script));
    }

    [Test]
    public void Process_BlockComment()
    {
        var queue  = new Queue();
        var parser = new SeedModuleParser(queue);

        var script = Lines(
            "/* A multi-line block comment.",
            "--# MODULE: Not a module! This is in a block comment. */",
            "/* Another multi-line block comment.",
            "--# MODULE: Not a module! This is in an unterminated block comment."
        );

        parser.Process(script);
        parser.Complete();

        Assert(queue, Module("(init)", script));
    }

    [Test]
    public void Process_LineComment()
    {
        var queue  = new Queue();
        var parser = new SeedModuleParser(queue);

        var script = " --# MODULE: Not a module! This is a deceptive line comment.";

        parser.Process(script);
        parser.Complete();

        Assert(queue, Module("(init)", script));
    }

    [Test]
    public void Process_Module()
    {
        var queue  = new Queue();
        var parser = new SeedModuleParser(queue);

        parser.Process(Lines(
            "--# MODULE: a",
            "batch A",
            "--#\t MODULE:\t b\t ",
            "batch B",
            "--#MODULE:c",
            "--# MODULE: \0\v!\"#$%&'()*+,-./0:;<=>?@A[\\]^_`a{|}~\u00FF\U0001F4A9 "
        ));
        parser.Complete();

        Assert(
            queue,
            Module("(init)"),
            Module("a", Lines("batch A", "")),
            Module("b", Lines("batch B", "")),
            Module("c"),
            Module("\0\v!\"#$%&'()*+,-./0:;<=>?@A[\\]^_`a{|}~\u00FF\U0001F4A9")
        );
    }

    [Test]
    public void Process_Module_NoName()
    {
        var queue  = new Queue();
        var parser = new SeedModuleParser(queue);

        parser
            .Invoking(p => p.Process("--# MODULE:"))
            .Should().Throw<FormatException>();
    }

    [Test]
    public void Process_Module_MultipleNames()
    {
        var queue  = new Queue();
        var parser = new SeedModuleParser(queue);

        parser
            .Invoking(p => p.Process("--# MODULE: a b"))
            .Should().Throw<FormatException>();
    }

    [Test]
    public void Process_ProvidesAndRequires()
    {
        var queue  = new Queue();
        var parser = new SeedModuleParser(queue);

        parser.Process(Lines(
            "--# MODULE: a",
            "--# REQUIRES: x y z",
            "batch A",
            "--# MODULE: b",
            "--# PROVIDES: x   z",
            "batch B",
            "--# MODULE: c",
            "--# PROVIDES:   y z",
            "batch C"
        ));
        parser.Complete();

        Assert(
            queue,
            Module("(init)"),
            Module("b", Lines("--# PROVIDES: x   z", "batch B", "")),
            Module("c", Lines("--# PROVIDES:   y z", "batch C"    )),
            Module("a", Lines("--# REQUIRES: x y z", "batch A", ""))
        );
    }

    private string Lines(params string[] lines)
        => string.Join(_newLine, lines);

    private static (string, string[]) Module(string name, params string[] batches)
        => (name, batches);

    private static void Assert(Queue queue, params (string, string[])[] expected)
    {
        var actual = Enumerate(queue).ToList();

        actual.Should().HaveCount(expected.Length);

        for (var i = 0; i < expected.Length; i++)
        {
            var (name, batches) = expected[i];
            actual[i].Name .Should().Be   (name);
            actual[i].Value.Should().Equal(batches);
        }
    }

    private static IEnumerable<Module> Enumerate(Queue queue)
    {
        queue.Validate().Should().BeEmpty();

        for (;;)
        {
            var module = queue.TryDequeue();
            if (module is null)
                yield break;

            yield return module;
            queue.Complete(module);
        }
    }
}
