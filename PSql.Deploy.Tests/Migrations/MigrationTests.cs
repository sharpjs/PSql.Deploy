// Copyright Subatomix Research Inc.
// SPDX-License-Identifier: MIT

namespace PSql.Deploy.Migrations;

[TestFixture]
public class MigrationTests
{
    [Test]
    public void Construct_NullMigration()
    {
        Should.Throw<ArgumentNullException>(() =>
        {
            _ = new Migration(null!);
        });
    }

    [Test]
    public void Name_Get()
    {
        var inner = new M.Migration("TestMigration");

        var migration = new Migration(inner);

        migration.Name.ShouldBe("TestMigration");
    }

    [Test]
    public void IsPseudo_Get()
    {
        var inner = new M.Migration("_Begin");

        var migration = new Migration(inner);

        migration.IsPseudo.ShouldBeTrue();
    }

    [Test]
    public void Path_Get()
    {
        const string Path = "/test/m/_Main.sql";

        var inner = new M.Migration("m") { Path = Path };

        var migration = new Migration(inner);

        migration.Path.ShouldBe(Path);
    }

    [Test]
    public void Hash_Get()
    {
        const string Hash = "0123456789abcdef";

        var inner = new M.Migration("m") { Hash = Hash };

        var migration = new Migration(inner);

        migration.Hash.ShouldBe(Hash);
    }

    [Test]
    public void State_Get()
    {
        var inner = new M.Migration("m") { State = M.MigrationState.AppliedPre };

        var migration = new Migration(inner);

        migration.State.ShouldBe(MigrationState.AppliedPre);
    }
}
