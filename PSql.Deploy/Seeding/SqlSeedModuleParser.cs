using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using DependencyQueue;

namespace PSql.Deploy.Seeding
{
    using static RegexOptions;

    using Queue   = DependencyQueue             <IEnumerable<string>>;
    using Builder = DependencyQueueEntryBuilder <IEnumerable<string>>;

    /// <summary>
    ///   Discovers seed modules and adds them to a dependency queue.
    /// </summary>
    public class SqlSeedModuleParser
    {
        /// <summary>
        ///   The name of the seed module that is the initial current module
        ///   for newly-created <see cref="SqlSeedModuleParser"/> instances.
        /// </summary>
        public const string InitialModuleName = "(init)";

        // Builder for seed modules (DependencyQueue entries)
        private readonly Builder _builder;

        // Batches in the current seed module (DependencyQueue entry)
        private List<string> _batches;

        /// <summary>
        ///   Initializes a new <see cref="SqlSeedModuleParser"/> that adds
        ///   seed modules to the specified dependency queue.
        /// </summary>
        /// <param name="queue">
        ///   The dependency queue to which to add discovered seed modules.
        /// </param>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="queue"/> is <see langword="null"/>.
        /// </exception>
        public SqlSeedModuleParser(Queue queue)
        {
            if (queue is null)
                throw new ArgumentNullException(nameof(queue));

            NewModule(
                _builder = queue.CreateEntryBuilder(),
                InitialModuleName,
                out _batches
            );
        }

        /// <summary>
        ///   Discovers seed modules in the specified string and adds them to
        ///   the dependency queue.
        /// </summary>
        /// <param name="text">
        ///   The text in which to discover seed modules.
        /// </param>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="text"/> is <see langword="null"/>.
        /// </exception>
        public void Process(string text)
        {
            if (text is null)
                throw new ArgumentNullException(nameof(text));
            if (text.Length == 0)
                return;

            var index = 0;

            for (;;)
            {
                // Find the next non-token block and the next token
                var start = index;
                var match = TokenRegex.Match(text, index);

                // Detect end of input; handle final batch
                if (!match.Success)
                {
                    AddBatch(text, start);
                    EndModule();
                    return;
                }

                // Compute where next iteration will start
                index = match.Index + match.Length;

                // All tokens except line comment are inert
                if (match.Value[0] != '-')
                    continue;

                // Non-magic line comments also are inert
                if (!match.Groups.TryGetValue("cmd", out var command))
                    continue;

                // Recognized magic comment
                var arguments = match.Groups["arg"].Captures;

                // Dispatch magic comment
                switch (command.Value[0])
                {
                    // MODULE
                    case 'M': case 'm':
                        AddBatch(text, start, match.Index);
                        EndModule();
                        NewModule(arguments);
                        break;

                    // PROVIDES
                    case 'P': case 'p':
                        AddProvides(arguments);
                        break;

                    // REQUIRES
                    case 'R': case 'r':
                        AddRequires(arguments);
                        break;
                }
            }
        }

        private void EndModule()
        {
            _builder.Enqueue();
        }

        private void AddBatch(ReadOnlySpan<char> text)
        {
            text = text.Trim(SpaceCharacters);

            if (text.Length != 0)
                _batches.Add(new string(text));
        }

        private void AddBatch(ReadOnlySpan<char> text, int start)
        {
            AddBatch(text.Slice(start));
        }

        private void AddBatch(ReadOnlySpan<char> text, int start, int end)
        {
            AddBatch(text.Slice(start, length: end - start));
        }

        private void NewModule(CaptureCollection arguments)
        {
            if (arguments.Count != 1)
                throw new Exception(); // TODO

            NewModule(_builder, arguments[0].Value, out _batches);
        }

        private void AddProvides(CaptureCollection arguments)
        {
            _builder.AddProvides(arguments.Select(a => a.Value));
        }

        private void AddRequires(CaptureCollection arguments)
        {
            _builder.AddRequires(arguments.Select(a => a.Value));
        }

        private static void NewModule(Builder builder, string name, out List<string> batches)
        {
            builder.NewEntry(name, batches = new());
        }

        private static readonly string SpaceCharacters = " \t";

        private static readonly Regex TokenRegex = new Regex(
            @"
                '     ( [^']  | ''   )*                                 ( '     | \z ) | # string
                \[    ( [^\]] | \]\] )*                                 ( \]    | \z ) | # quoted identifier
                /\*   ( .     | \n   )*?                                ( \*/   | \z ) | # block comment
                ^--\# [ \t]*   (?<cmd>MODULE|PROVIDES|REQUIRES)                          # magic comment
                    : [ \t]* ( (?<arg>([^ \t\r\n]|\r(?!\n))+) [ \t]* )* ( \r?\n | \z ) | # + arguments
                --    .*?                                               ( \r?\n | \z )   # line comment
            ",
            Options
        );

        private const RegexOptions Options
            = Multiline
            | IgnoreCase
            | CultureInvariant
            | IgnorePatternWhitespace
            | ExplicitCapture
            | Compiled;
    }
}
