// Copyright 2023 Subatomix Research Inc.
// SPDX-License-Identifier: ISC

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
        Invoking(() => MigrationLoader.LoadContent(null!))
            .Should().Throw<ArgumentNullException>();
    }

    [Test]
    public void LoadContent_NullPath()
    {
        Invoking(() => MigrationLoader.LoadContent(new("m")))
            .Should().ThrowExactly<ArgumentException>()
            .WithMessage("Migration must have a path*");
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

        migration.Pre .Sql.Should().StartWith("DECLARE @__sql__");
        migration.Core.Sql.Should().StartWith("DECLARE @__sql__");
        migration.Post.Sql.Should().StartWith("DECLARE @__sql__");

        migration.Pre .IsRequired.Should().BeFalse();
        migration.Core.IsRequired.Should().BeFalse();
        migration.Post.IsRequired.Should().BeFalse();

        migration.DependsOn      .Should().BeEmpty();
        migration.IsContentLoaded.Should().BeTrue();
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

        migration.Pre .Sql.Should().ContainAll("DECLARE @__sql__",         "pre0",  "pre1" );
        migration.Core.Sql.Should().ContainAll("DECLARE @__sql__", "init", "core0", "core1");
        migration.Post.Sql.Should().ContainAll("DECLARE @__sql__",         "post0", "post1");

        migration.Pre .IsRequired.Should().BeTrue();
        migration.Core.IsRequired.Should().BeTrue();
        migration.Post.IsRequired.Should().BeTrue();

        migration.DependsOn      .Should().BeEmpty();
        migration.IsContentLoaded.Should().BeTrue();
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

        migration.Pre .Sql.Should().StartWith("DECLARE @__sql__").And.Contain("text");
        migration.Core.Sql.Should().BeEmpty();
        migration.Post.Sql.Should().BeEmpty();

        migration.Pre .IsRequired.Should().BeTrue();
        migration.Core.IsRequired.Should().BeFalse();
        migration.Post.IsRequired.Should().BeFalse();

        migration.DependsOn      .Should().BeEmpty();
        migration.IsContentLoaded.Should().BeTrue();
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

        migration.Pre .Sql.Should().BeEmpty();
        migration.Core.Sql.Should().BeEmpty();
        migration.Post.Sql.Should().StartWith("DECLARE @__sql__").And.Contain("text");

        migration.Pre .IsRequired.Should().BeFalse();
        migration.Core.IsRequired.Should().BeFalse();
        migration.Post.IsRequired.Should().BeTrue();

        migration.DependsOn      .Should().BeEmpty();
        migration.IsContentLoaded.Should().BeTrue();
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

        migration.Pre .Sql.Should().StartWith ("DECLARE @__sql__");
        migration.Core.Sql.Should().ContainAll("DECLARE @__sql__", "foo", "bar", "baz", "quux");
        migration.Post.Sql.Should().StartWith ("DECLARE @__sql__");

        migration.Pre .IsRequired.Should().BeFalse();
        migration.Core.IsRequired.Should().BeTrue();
        migration.Post.IsRequired.Should().BeFalse();

        migration.DependsOn.Should().BeEquivalentTo(new[]
        {
            new MigrationReference("a"),
            new MigrationReference("b"),
            new MigrationReference("c")
        });

        migration.IsContentLoaded.Should().BeTrue();
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

        migration.Pre .Sql.Should().StartWith ("DECLARE @__sql__");
        migration.Core.Sql.Should().ContainAll("DECLARE @__sql__", "foo", "--# WAT", "bar");
        migration.Post.Sql.Should().StartWith ("DECLARE @__sql__");

        migration.Pre .IsRequired.Should().BeFalse();
        migration.Core.IsRequired.Should().BeTrue();
        migration.Post.IsRequired.Should().BeFalse();

        migration.DependsOn      .Should().BeEmpty();
        migration.IsContentLoaded.Should().BeTrue();
    }

    private string BasePath => _basePath ??= MakeDirectory();
    private string? _basePath;

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

    private string MakeFile(string name, string content)
    {
        var path = Path.Combine(BasePath, name);
        File.WriteAllText(path, content);
        return path;
    }

    protected override void CleanUp(bool managed)
    {
        DeleteDirectory();
        base.CleanUp(managed);
    }

    private void DeleteDirectory()
    {
        if (_basePath is null)
            return;

        try
        {
            Directory.Delete(_basePath, recursive: true);
        }
        catch { } // best effort only
    }
}
