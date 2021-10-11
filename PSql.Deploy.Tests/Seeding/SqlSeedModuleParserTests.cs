using System;
using System.Collections.Generic;
using System.Linq;
using DependencyQueue;
using FluentAssertions;
using NUnit.Framework;

namespace PSql.Deploy.Seeding
{
    using static FluentActions;

    using Module = DependencyQueueEntry<IEnumerable<string>>;
    using Queue  = DependencyQueue<IEnumerable<string>>;

    [TestFixture]
    public class SqlSeedModuleParserTests
    {
        [Test]
        public void Construct_NullQueue()
        {
            Invoking(() => new SqlSeedModuleParser(null!))
                .Should().Throw<ArgumentNullException>()
                .Where(e => e.ParamName == "queue");
        }

        [Test]
        public void Process_NullText()
        {
            var queue = new Queue();

            new SqlSeedModuleParser(queue)
                .Invoking(p => p.Process(null!))
                .Should().Throw<ArgumentNullException>()
                .Where(e => e.ParamName == "text");
        }

        [Test]
        public void Process_Init()
        {
            var queue  = new Queue();
            var parser = new SqlSeedModuleParser(queue);

            parser.Process("a");

            Assert(queue, Module("(init)", "a"));
        }

        [Test]
        [TestCaseSource(nameof(LineEndings))]
        public void Process_InertTokens(string eol)
        {
            var queue  = new Queue();
            var parser = new SqlSeedModuleParser(queue);

            var script = string.Concat(
                "'",                                                         eol,
                "--# MODULE: Not a module! This is in a string.'",           eol,
                "b [",                                                       eol,
                "--# MODULE: Not a module! This is in a quoted identifer.]", eol,
                "c /*",                                                      eol,
                "-# MODULE: Not a module! This is in a block comment */",    eol,
                "d",                                                         eol,
                "-- MODULE: Not a module! This is just a line comment.",     eol,
                "e",                                                         eol
            );

            parser.Process(script);

            Assert(queue, Module("(init)", script));
        }

        [Test]
        [TestCaseSource(nameof(LineEndings))]
        public void Process_Module(string eol)
        {
            var queue  = new Queue();
            var parser = new SqlSeedModuleParser(queue);

            parser.Process(string.Concat(
                "--# MODULE: a", eol,
                "one",           eol,
                "--# MODULE: b", eol,
                "two",           eol
            ));

            Assert(
                queue,
                Module("(init)"),
                Module("a", "one" + eol),
                Module("b", "two" + eol)
            );
        }

        [Test]
        [TestCaseSource(nameof(LineEndings))]
        public void Process_ProvidesAndRequires(string eol)
        {
            var queue  = new Queue();
            var parser = new SqlSeedModuleParser(queue);

            parser.Process(string.Concat(
                "--# MODULE:   a", eol,
                "--# REQUIRES: x", eol,
                "one",             eol,
                "--# MODULE:   b", eol,
                "--# PROVIDES: x", eol,
                "two",             eol
            ));

            Assert(
                queue,
                Module("(init)"),
                Module("b", string.Concat("--# PROVIDES: x", eol, "two", eol)),
                Module("a", string.Concat("--# REQUIRES: x", eol, "one", eol))
            );
        }

        public static IEnumerable<string> LineEndings { get; }
            = new[] { "\n", "\r\n" };

        private static void
            Assert(
                Queue                                    queue,
                params (string Name, string[] Batches)[] expected
            )
        {
            var actual = Enumerate(queue).ToList();

            actual.Should().HaveCount(expected.Length);

            for (var i = 0; i < expected.Length; i++)
            {
                actual[i].Name .Should().Be   (expected[i].Name);
                actual[i].Value.Should().Equal(expected[i].Batches);
            }
        }

        private static (string Name, string[] Batches)
            Module(
                string          name,
                params string[] batches
            )
            => (name, batches);

        private static IEnumerable<Module> Enumerate(Queue queue)
        {
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
}
