// Copyright Subatomix Research Inc.
// SPDX-License-Identifier: MIT

using System.Text.RegularExpressions;
using Prequel;

namespace PSql.Deploy.Seeds;

using static RegexOptions;

internal class SeedLoader
{
    public static LoadedSeed Load(Seed seed, IEnumerable<(string, string)>? defines = null)
    {
        if (seed is null)
            throw new ArgumentNullException(nameof(seed));

        var loader = new SeedLoader();

        foreach (var batch in Preprocess(seed, defines))
            loader.Process(batch);

        return new(seed, loader.Complete());
    }

    private static IEnumerable<string> Preprocess(
        Seed                           seed,
        IEnumerable<(string, string)>? defines = null)
    {
        var directoryPath = Path.GetDirectoryName(seed.Path)!;
        var fileName      = Path.GetFileName     (seed.Path)!;

        var preprocessor = new SqlCmdPreprocessor
        {
            Variables = { ["Path"] = directoryPath }
        };

        if (defines is not null)
            foreach (var (key, value) in defines)
                preprocessor.Variables[key] = value;

        var raw = File.ReadAllText(seed.Path);

        return preprocessor.Process(raw, fileName);
    }

    private readonly ImmutableArray<SeedModule>.Builder _modules;
    private readonly ImmutableArray<string>    .Builder _batches;
    private readonly ImmutableArray<string>    .Builder _provides;
    private readonly ImmutableArray<string>    .Builder _requires;

    private const string InitialModuleName = "(init)";

    private string _moduleName = InitialModuleName;

    private SeedLoader()
    {
        _modules  = ImmutableArray.CreateBuilder<SeedModule>();
        _batches  = ImmutableArray.CreateBuilder<string>();
        _provides = ImmutableArray.CreateBuilder<string>();
        _requires = ImmutableArray.CreateBuilder<string>();
    }

    private void Process(string text)
    {
#if BECOMES_PUBLIC_API
        if (text is null)
            throw new ArgumentNullException(nameof(text));
        if (text.Length == 0)
            return;
#endif

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

    public ImmutableArray<SeedModule> Complete()
    {
        EndModule();

        return _modules.ToImmutable();
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

        _moduleName = arguments[0].Value;
    }

    private void AddProvides(CaptureCollection arguments)
    {
        _provides.AddRange(arguments.Select(a => a.Value));
    }

    private void AddRequires(CaptureCollection arguments)
    {
        _requires.AddRange(arguments.Select(a => a.Value));
    }

    private void AddBatch(string text, int start)
    {
        text = text[start..];

        if (text.Length != 0)
            _batches.Add(text);
    }

    private void AddBatch(string text, int start, int index)
    {
        text = text[start..index];

        if (text.Length != 0)
            _batches.Add(text);
    }

    private void EndModule()
    {
        _modules.Add(new(
            _moduleName,
            _batches .ToImmutable(),
            _provides.ToImmutable(),
            _requires.ToImmutable()
        ));

        _moduleName = InitialModuleName;
        _batches .Clear();
        _provides.Clear();
        _requires.Clear();
    }

    private static readonly Regex TokenRegex = new(
        """
        '     ( [^']  | ''   )*                             ( '     | \z ) | # string
        \[    ( [^\]] | \]\] )*                             ( \]    | \z ) | # quoted identifier
        /\*   ( .     | \n   )*?                            ( \*/   | \z ) | # block comment
        ^--\# [ \t]* (?<cmd> MODULE | PROVIDES | REQUIRES ) :                # magic comment
              [ \t]* (                                                       #   followed by
                (?<arg> ( [^ \t\r\n] | \r(?!\n) )+ ) [ \t]*                  #   arguments
              )*                                            ( \r?\n | \z )
        """,
        IgnoreCase              |
        IgnorePatternWhitespace |
        Multiline               |
        ExplicitCapture         |
        CultureInvariant        |
        Compiled
    );
} 
