// Copyright Subatomix Research Inc.
// SPDX-License-Identifier: MIT

namespace PSql.Deploy.Migrations;

[TestFixture]
public class MigrationDiscovererTests
{
    [Test]
    public void GetAll_NullPath()
    {
        Should.Throw<ArgumentNullException>(static () =>
        {
            return MigrationDiscoverer.GetAll(null!);
        })
        .ParamName.ShouldBe("path");
    }

    [Test]
    public void GetAll_DirectoryDoesNotExist()
    {
        var path = Path.Combine(TestDirectory, "DoesNotExist");

        var migrations = MigrationDiscoverer.GetAll(path);

        migrations.ShouldBeEmpty();
    }

    [Test]
    public void GetAll_NoMigrationsInDirectory()
    {
        var path = Path.Combine(TestDirectory, "TestDbs");

        var migrations = MigrationDiscoverer.GetAll(path);

        migrations.ShouldBeEmpty();
    }

    [Test]
    public void GetAll_Normal()
    {
        var path = Path.Combine(TestDirectory, "TestDbs", "A");

        var migrations = MigrationDiscoverer.GetAll(path);

        migrations.Length.ShouldBe(5);

        migrations[0].ShouldSatisfyAllConditions(
            m => m.Name    .ShouldBe("_Begin"),
            m => m.Path    .ShouldBe(MigrationPath("_Begin")),
            m => m.Hash    .ShouldBe("1FA5BB132E282E92E49B1F3C73E5CA91977D1B7A"),
            m => m.IsPseudo.ShouldBeTrue()
        );

        migrations[1].ShouldSatisfyAllConditions(
            m => m.Name    .ShouldBe("Migration0"),
            m => m.Path    .ShouldBe(MigrationPath("Migration0")),
            m => m.Hash    .ShouldBe("D8462C316FD72659FB11FA7C9727D05707F8332B"),
            m => m.IsPseudo.ShouldBeFalse()
        );

        migrations[2].ShouldSatisfyAllConditions(
            m => m.Name    .ShouldBe("Migration1"),
            m => m.Path    .ShouldBe(MigrationPath("Migration1")),
            m => m.Hash    .ShouldBe("2909F7C67C9B831FFCD4655F31683941F700A205"),
            m => m.IsPseudo.ShouldBeFalse()
        );

        migrations[3].ShouldSatisfyAllConditions(
            m => m.Name    .ShouldBe("Migration2"),
            m => m.Path    .ShouldBe(MigrationPath("Migration2")),
            m => m.Hash    .ShouldBe("FB049E6EA9DC10019088850C94E9C5D2661A6DE7"),
            m => m.IsPseudo.ShouldBeFalse()
        );

        migrations[4].ShouldSatisfyAllConditions(
            m => m.Name    .ShouldBe("_End"),
            m => m.Path    .ShouldBe(MigrationPath("_End")),
            m => m.Hash    .ShouldBe("48E2F54503C2A75955805121FD5C24E2DE586489"),
            m => m.IsPseudo.ShouldBeTrue()
        );
    }

    [Test]
    public void GetAll_Normal_WithMaxName()
    {
        var path = Path.Combine(TestDirectory, "TestDbs", "A");

        var migrations = MigrationDiscoverer.GetAll(path, maxName: "Migration0");

        migrations.Length.ShouldBe(3);

        migrations[0].ShouldSatisfyAllConditions(
            m => m.Name    .ShouldBe("_Begin"),
            m => m.Path    .ShouldBe(MigrationPath("_Begin")),
            m => m.Hash    .ShouldBe("1FA5BB132E282E92E49B1F3C73E5CA91977D1B7A"),
            m => m.IsPseudo.ShouldBeTrue()
        );

        migrations[1].ShouldSatisfyAllConditions(
            m => m.Name    .ShouldBe("Migration0"),
            m => m.Path    .ShouldBe(MigrationPath("Migration0")),
            m => m.Hash    .ShouldBe("D8462C316FD72659FB11FA7C9727D05707F8332B"),
            m => m.IsPseudo.ShouldBeFalse()
        );

        migrations[2].ShouldSatisfyAllConditions(
            m => m.Name    .ShouldBe("_End"),
            m => m.Path    .ShouldBe(MigrationPath("_End")),
            m => m.Hash    .ShouldBe("48E2F54503C2A75955805121FD5C24E2DE586489"),
            m => m.IsPseudo.ShouldBeTrue()
        );
    }

    private string MigrationPath(string name)
        => Path.Combine(Path.Combine(TestDirectory, "TestDbs", "A", "Migrations", name, "_Main.sql"));

    private string TestDirectory { get; }
        = TestContext.CurrentContext.TestDirectory;
}
