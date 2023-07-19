// Copyright 2023 Subatomix Research Inc.
// SPDX-License-Identifier: ISC

using System.Text.RegularExpressions;
using Prequel;

namespace PSql.Deploy.Migrations;

using static RegexOptions;

internal static class MigrationLoader
{
    internal static void LoadContent(Migration migration)
    {
        if (migration is null)
            throw new ArgumentNullException(nameof(migration));
        if (migration.Path is null)
            throw new ArgumentException("Migration must have a path.", nameof(migration));

        if (migration.IsContentLoaded)
            return;

        LoadContentInternal(migration);
    }

    internal static void LoadContentInternal(Migration migration)
    {
        lock (migration)
        {
            if (migration.IsContentLoaded)
                return;

            LoadContentCore(migration);
        }
    }

    private static void LoadContentCore(Migration migration)
    {
        var pre     = new SqlErrorHandlingBuilder();
        var core    = new SqlErrorHandlingBuilder();
        var post    = new SqlErrorHandlingBuilder();
        var depends = new SortedSet<string>(StringComparer.OrdinalIgnoreCase);

        AppendAuthoredSql(migration, pre, core, post, depends);

        // A phase is required if it contains authored SQL
        migration.Pre .IsRequired = !pre .IsEmpty;
        migration.Core.IsRequired = !core.IsEmpty;
        migration.Post.IsRequired = !post.IsEmpty;

        AppendFinalBatches(migration, pre,  MigrationPhase.Pre );
        AppendFinalBatches(migration, core, MigrationPhase.Core);
        AppendFinalBatches(migration, post, MigrationPhase.Post);

        migration.Pre .Sql  = pre .Complete();
        migration.Core.Sql  = core.Complete();
        migration.Post.Sql  = post.Complete();
        migration.DependsOn = MakeDependsOn(depends);

        migration.IsContentLoaded = true;
    }

    private static void AppendAuthoredSql(
        Migration               migration,
        SqlErrorHandlingBuilder pre,
        SqlErrorHandlingBuilder core,
        SqlErrorHandlingBuilder post,
        SortedSet<string>       depends)
    {
        var current = migration.Name switch
        {
            "_Begin" => pre,
            "_End"   => post,
            _        => core,
        };

        foreach (var batch in Preprocess(migration))
        {
            current.StartNewBatch();

            foreach (var chunk in (IEnumerable<Match>) ChunksRe.Matches(batch))
            {
                // Text chunk belongs to block in progress
                current.Append(batch, chunk.Groups["text"]);

                // Detect EOF
                var magic = chunk.Groups["magic"];
                if (magic.Value.IsNullOrEmpty())
                    break;

                // Split magic comment, if any
                var match = CommandRe.Match(chunk.Groups["cmd"].Value);
                var name  = match.Groups["name"].Value;
                var args  = match.Groups["args"].Captures.Select(c => c.Value);

                // Interpret magic comment, if any
                switch (name)
                {
                    case "PRE":  current = pre;  break;
                    case "CORE": current = core; break;
                    case "POST": current = post; break;

                    case "REQUIRES" when !migration.IsPseudo:
                        // Pseudo-migrations cannot declare dependencies
                        depends.UnionWith(args);
                        break;

                    default:
                        // Not our magic
                        current.Append(batch, magic);
                        break;
                }
            }
        }
    }

    private static IEnumerable<string> Preprocess(Migration migration)
    {
        var directoryPath = Path.GetDirectoryName(migration.Path)!;
        var fileName      = Path.GetFileName     (migration.Path)!;

        var preprocessor = new SqlCmdPreprocessor
        {
            Variables = { ["Path"] = directoryPath }
        };

        var raw = File.ReadAllText(migration.Path);

        return preprocessor.Process(raw, fileName);
    }

    private static void AppendFinalBatches(
        Migration               migration,
        SqlErrorHandlingBuilder builder,
        MigrationPhase          phase)
    {
        if (migration.IsPseudo)
            return;

        var name = migration.Name.Replace("'", "''");
        var hash = migration.Hash.Replace("'", "''");

        builder.StartNewBatch();
        builder.Append(
            $"PRINT '+ data _deploy.Migration ({name} {phase} done)';"
        );
        builder.StartNewBatch();
        builder.Append(
            $"""
            MERGE _deploy.Migration dst
            USING
                (
                    SELECT
                        Name = '{name}',
                        Hash = '{hash}',
                        Date = SYSUTCDATETIME()
                ) src
            ON src.Name = dst.Name
            WHEN MATCHED
                THEN UPDATE SET
                    dst.Hash = src.Hash,
                    dst.{phase}RunDate = src.Date
            WHEN NOT MATCHED BY TARGET
                THEN INSERT
                    (Name, Hash, {phase}RunDate)
                VALUES
                    (Name, Hash, Date)
            ;

            IF @@ROWCOUNT != 1
                THROW 50000, 'Migration registration for {name} failed.', 0;
            """
        );
    }

    private static ImmutableArray<MigrationReference> MakeDependsOn(SortedSet<string> depends)
    {
        if (depends.Count == 0)
            return ImmutableArray<MigrationReference>.Empty;

        var array = ImmutableArray.CreateBuilder<MigrationReference>(depends.Count);

        foreach (var name in depends)
            array.Add(new(name));

        return array.MoveToImmutable();
    }

    private static readonly Regex ChunksRe = new(
        """
        # Start where previous match ended
        \G

        # Match a chunk of SQL text
        (?<text>
            (   [^'\[/-]                                # regular character
            |   '  ( [^']  | ''   )* ( '     | \z )     # string
            |   \[ ( [^\]] | \]\] )* ( \]    | \z )     # quoted identifier
            |   -- (?!\#) .*?        ( \r?\n | \z )     # line comment (non-magic)
            |   -  (?! -)                               # just a dash
            |   /  \*     .*?        ( \* /  | \z )     # block comment
            |   /  (?!\*)                               # just a slash
            )*
        )

        # Match a magic comment or end of file
        (?<magic>
            (   ^ --\# (?<cmd> .*? ) ( \r?\n | \z )     # magic comment
            |   \z                                      # end of file
            )
        )
        """,
        Multiline               |   // m: ^/$ match BOL/EOL
        ExplicitCapture         |   // n: named captures only
        Singleline              |   // s: . includes \n
        IgnorePatternWhitespace |   // x: ignore pattern whitespace
        CultureInvariant        |   //    invariant comparison
        Compiled                    //    compile to an assembly
    );

    private static readonly Regex CommandRe = new(
        """
        \A             [\x20\t]*                        # beginning of string
        (?<name> \w+ ) [\x20\t]*                        # NAME
        (
            ( :                     [\x20\t]* )         # NAME:
            ( (?<args> [^\x20\t]+ ) [\x20\t]* )*        # NAME: ARG ARG ARG
        )?
        \z                                              # end of string
        """,
        ExplicitCapture         |   // n: named captures only
        Singleline              |   // s: . includes \n
        IgnorePatternWhitespace |   // x: ignore pattern whitespace
        CultureInvariant        |   //    invariant comparison
        Compiled                    //    compile to an assembly
    );
}
