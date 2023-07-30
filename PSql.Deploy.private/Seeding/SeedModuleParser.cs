// Copyright 2023 Subatomix Research Inc.
// SPDX-License-Identifier: ISC

using System.Text.RegularExpressions;
using DependencyQueue;

namespace PSql.Deploy.Seeding;

using static RegexOptions;

using Queue   = DependencyQueue             <IEnumerable<string>>;
using Builder = DependencyQueueEntryBuilder <IEnumerable<string>>;

/// <summary>
///   Discovers seed modules and adds them to a dependency queue.
/// </summary>
internal class SeedModuleParser
{
    /// <summary>
    ///   The name of the seed module that is the initial current module for
    ///   newly-created <see cref="SeedModuleParser"/> instances.
    /// </summary>
    public const string InitialModuleName = "(init)";

    // Builder for seed modules (DependencyQueue entries)
    private readonly Builder _builder;

    // Batches in the current seed module (DependencyQueue entry)
    private List<string> _batches;
    private bool         _isCompleted;

    /// <summary>
    ///   Initializes a new <see cref="SeedModuleParser"/> that adds seed
    ///   modules to the specified dependency queue.
    /// </summary>
    /// <param name="queue">
    ///   The dependency queue to which to add discovered seed modules.
    /// </param>
    /// <exception cref="ArgumentNullException">
    ///   <paramref name="queue"/> is <see langword="null"/>.
    /// </exception>
    public SeedModuleParser(object queue)
        : this((Queue) queue) { }

    private SeedModuleParser(Queue queue)
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
    ///   Gets whether the seed module parser is in the completed state.
    /// </summary>
    public bool IsCompleted => _isCompleted;

    /// <summary>
    ///   Discovers seed modules in the specified string and adds them to the
    ///   dependency queue.
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
        if (_isCompleted)
            throw OnCompleted();

        var start = 0;
        var index = 0;

        for (;;)
        {
            // Find the next non-token block and the next token
            var match = TokenRegex.Match(text, index);

            // Detect end of input; handle final batch
            if (!match.Success)
            {
                AddBatch(text, start);
                return;
            }

            // Compute where next iteration will start
            index = match.Index + match.Length;

            // All tokens except magic comment are inert
            if (match.Value[0] != '-')
                continue;

            // Recognized a magic comment
            start = HandleMagicComment(text, start, index, match);
        }
    }

    public void Complete()
    {
        if (_isCompleted)
            throw OnCompleted();

        EndModule();
        _isCompleted = true;
    }

    private int HandleMagicComment(string text, int start, int index, Match match)
    {
        // Decode
        var command   = match.Groups["cmd"];
        var arguments = match.Groups["arg"].Captures;

        // Dispatch
        switch (command.Value[0])
        {
            // MODULE
            case 'M':
            case 'm':
                AddBatch(text, start, match.Index);
                EndModule();
                NewModule(arguments);
                start = index;
                break;

            // PROVIDES
            case 'P':
            case 'p':
                AddProvides(arguments);
                break;

            // REQUIRES
            case 'R':
            case 'r':
                AddRequires(arguments);
                break;
        }

        return start;
    }

    private void NewModule(CaptureCollection arguments)
    {
        if (arguments.Count != 1)
            throw new FormatException("The MODULE magic comment expects exactly one argument.");

        NewModule(_builder, arguments[0].Value, out _batches);
    }

    private static void NewModule(Builder builder, string name, out List<string> batches)
    {
        builder.NewEntry(name, batches = new());
    }

    private void EndModule()
    {
        _builder.Enqueue();
    }

    private void AddBatch(ReadOnlySpan<char> text)
    {
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

    private void AddProvides(CaptureCollection arguments)
    {
        _builder.AddProvides(arguments.Select(a => a.Value));
    }

    private void AddRequires(CaptureCollection arguments)
    {
        _builder.AddRequires(arguments.Select(a => a.Value));
    }

    private static InvalidOperationException OnCompleted()
    {
        return new("The " + nameof(SeedModuleParser) + " has completed and is no longer usable.");
    }

    private static readonly Regex TokenRegex = new Regex(
        @"
                '     ( [^']  | ''   )*                                 ( '     | \z ) | # string
                \[    ( [^\]] | \]\] )*                                 ( \]    | \z ) | # quoted identifier
                /\*   ( .     | \n   )*?                                ( \*/   | \z ) | # block comment
                ^--\# [ \t]*   (?<cmd>MODULE|PROVIDES|REQUIRES)                          # magic comment
                    : [ \t]* ( (?<arg>([^ \t\r\n]|\r(?!\n))+) [ \t]* )* ( \r?\n | \z )   # + arguments
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
