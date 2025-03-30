// Copyright Subatomix Research Inc.
// SPDX-License-Identifier: MIT

namespace PSql.Deploy.Migrations;

[TestFixture]
public class MigrationLoaderTests : TestHarnessBase
{
    // Inputs:
    // - Name
    // - Hash
    // - Path
    // - IsPseudo
    // - IsContentLoaded
    // Outputs:
    // - Pre.Sql
    // - Core.Sql
    // - Post.Sql
    // - DependsOn
    // - IsContentLoaded

    [Test]
    public void LoadContent_Null()
    {
        Should.Throw<ArgumentNullException>(static () =>
        {
            MigrationLoader.LoadContent(null!);
        });
    }

    [Test]
    public void LoadContent_NullPath()
    {
        var e = Should.Throw<ArgumentException>(static () =>
        {
            MigrationLoader.LoadContent(new("m"));
        });

        e.Message.ShouldStartWith("Migration must have a path.");
    }

    [Test]
    public void LoadContent_AlreadyLoaded()
    {
        var migration = new Migration("m")
        {
            Path            = "/nonexistent",
            IsContentLoaded = true,
        };

        // Cover both checks
        MigrationLoader.LoadContent        (migration);
        MigrationLoader.LoadContentInternal(migration);
    }

    [Test]
    public void LoadContent_Empty()
    {
        var migration = new Migration("m")
        {
            Path = MakeFile("_Main.sql", ""),
        };

        MigrationLoader.LoadContent(migration);

        migration.Pre .Sql.ShouldStartWith("DECLARE @__sql__");
        migration.Core.Sql.ShouldStartWith("DECLARE @__sql__");
        migration.Post.Sql.ShouldStartWith("DECLARE @__sql__");

        migration.Pre .IsRequired.ShouldBeFalse();
        migration.Core.IsRequired.ShouldBeFalse();
        migration.Post.IsRequired.ShouldBeFalse();

        migration.DependsOn      .ShouldBeEmpty();
        migration.IsContentLoaded.ShouldBeTrue();
    }

    [Test]
    public void LoadContent_Normal()
    {
        var migration = new Migration("m")
        {
            Path = MakeFile(
                "_Main.sql",
                """
                init
                --# PRE
                pre0
                --# CORE
                core0
                --# POST
                post0
                --# PRE
                pre1
                --# CORE
                core1
                --# POST
                post1
                """
            ),
        };

        MigrationLoader.LoadContent(migration);

        migration.Pre .Sql.ShouldStartWith("DECLARE @__sql__");
        migration.Pre .Sql.ShouldContain  ("pre0");
        migration.Pre .Sql.ShouldContain  ("pre1");

        migration.Core.Sql.ShouldStartWith("DECLARE @__sql__");
        migration.Core.Sql.ShouldContain  ("init");
        migration.Core.Sql.ShouldContain  ("core0");
        migration.Core.Sql.ShouldContain  ("core1");

        migration.Post.Sql.ShouldStartWith("DECLARE @__sql__");
        migration.Post.Sql.ShouldContain  ("post0");
        migration.Post.Sql.ShouldContain  ("post1");

        migration.Pre .IsRequired.ShouldBeTrue();
        migration.Core.IsRequired.ShouldBeTrue();
        migration.Post.IsRequired.ShouldBeTrue();

        migration.DependsOn      .ShouldBeEmpty();
        migration.IsContentLoaded.ShouldBeTrue();
    }

    [Test]
    public void LoadContent_BeginPseudoMigration()
    {
        var migration = new Migration(Migration.BeginPseudoMigrationName)
        {
            Path = MakeFile(
                "_Main.sql",
                """
                text
                """
            ),
        };

        MigrationLoader.LoadContent(migration);

        migration.Pre .Sql.ShouldStartWith("DECLARE @__sql__");
        migration.Pre .Sql.ShouldContain("text");
        migration.Core.Sql.ShouldBeEmpty();
        migration.Post.Sql.ShouldBeEmpty();

        migration.Pre .IsRequired.ShouldBeTrue();
        migration.Core.IsRequired.ShouldBeFalse();
        migration.Post.IsRequired.ShouldBeFalse();

        migration.DependsOn      .ShouldBeEmpty();
        migration.IsContentLoaded.ShouldBeTrue();
    }

    [Test]
    public void LoadContent_EndPseudoMigration()
    {
        var migration = new Migration(Migration.EndPseudoMigrationName)
        {
            Path = MakeFile(
                "_Main.sql",
                """
                text
                """
            ),
        };

        MigrationLoader.LoadContent(migration);

        migration.Pre .Sql.ShouldBeEmpty();
        migration.Core.Sql.ShouldBeEmpty();
        migration.Post.Sql.ShouldStartWith("DECLARE @__sql__");
        migration.Post.Sql.ShouldContain("text");

        migration.Pre .IsRequired.ShouldBeFalse();
        migration.Core.IsRequired.ShouldBeFalse();
        migration.Post.IsRequired.ShouldBeTrue();

        migration.DependsOn      .ShouldBeEmpty();
        migration.IsContentLoaded.ShouldBeTrue();
    }

    [Test]
    public void LoadContent_DependsOn()
    {
        var migration = new Migration("m")
        {
            Path = MakeFile(
                "_Main.sql",
                """
                foo
                --# REQUIRES: c c a
                bar
                --# REQUIRES: 
                baz
                --# REQUIRES: a b
                quux
                """
            ),
        };

        MigrationLoader.LoadContent(migration);

        migration.Pre .Sql.ShouldStartWith("DECLARE @__sql__");

        migration.Core.Sql.ShouldStartWith("DECLARE @__sql__");
        migration.Core.Sql.ShouldContain  ("foo");
        migration.Core.Sql.ShouldContain  ("bar");
        migration.Core.Sql.ShouldContain  ("baz");
        migration.Core.Sql.ShouldContain  ("quux");

        migration.Post.Sql.ShouldStartWith("DECLARE @__sql__");

        migration.Pre .IsRequired.ShouldBeFalse();
        migration.Core.IsRequired.ShouldBeTrue();
        migration.Post.IsRequired.ShouldBeFalse();

        migration.DependsOn.ToArray().ShouldBeEquivalentTo(new[]
        {
            new MigrationReference("a"),
            new MigrationReference("b"),
            new MigrationReference("c")
        });

        migration.IsContentLoaded.ShouldBeTrue();
    }

    [Test]
    public void LoadContent_ForeignMagic()
    {
        var migration = new Migration("m")
        {
            Path = MakeFile(
                "_Main.sql",
                """
                foo
                --# WAT
                bar
                """
            ),
        };

        MigrationLoader.LoadContent(migration);

        migration.Pre .Sql.ShouldStartWith("DECLARE @__sql__");

        migration.Core.Sql.ShouldStartWith("DECLARE @__sql__");
        migration.Core.Sql.ShouldContain  ("foo");
        migration.Core.Sql.ShouldContain  ("--# WAT");
        migration.Core.Sql.ShouldContain  ("bar");

        migration.Post.Sql.ShouldStartWith("DECLARE @__sql__");

        migration.Pre .IsRequired.ShouldBeFalse();
        migration.Core.IsRequired.ShouldBeTrue();
        migration.Post.IsRequired.ShouldBeFalse();

        migration.DependsOn      .ShouldBeEmpty();
        migration.IsContentLoaded.ShouldBeTrue();
    }

    private string? _directoryPath;

    private string MakeFile(string name, string content)
    {
        var path = _directoryPath ??= MakeDirectory();

        path = Path.Combine(path, name);
        File.WriteAllText(path, content);
        return path;
    }

    private string MakeDirectory()
    {
        var context = TestContext.CurrentContext;

        var path = Path.Combine(
            context.WorkDirectory,
            nameof(MigrationLoaderTests),
            context.Test.Name
        );

        Directory.CreateDirectory(path);
        return path;
    }

    protected override void CleanUp(bool managed)
    {
        DeleteDirectory();
        base.CleanUp(managed);
    }

    private void DeleteDirectory()
    {
        if (_directoryPath is not { } path)
            return;

        try { Directory.Delete(path, recursive: true); }
        catch { } // best effort only
    }
}
