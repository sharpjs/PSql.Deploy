// Copyright Subatomix Research Inc.
// SPDX-License-Identifier: MIT

namespace PSql.Deploy.Migrations;

[TestFixture]
public partial class MigrationRepositoryTests
{
    [Test]
    public void GetLocalMigrations_DirectoryDoesNotExist()
    {
        var path = Path.Combine(TestDirectory, "DoesNotExist");

        var migrations = MigrationRepository.GetAll(path);

        migrations.ShouldBeEmpty();
    }

    [Test]
    public void GetLocalMigrations_NoMigrationsInDirectory()
    {
        var path = Path.Combine(TestDirectory, "TestDbs");

        var migrations = MigrationRepository.GetAll(path);

        migrations.ShouldBeEmpty();
    }

    [Test]
    public void GetLocalMigrations_Normal()
    {
        var path = Path.Combine(TestDirectory, "TestDbs", "A");

        var migrations = MigrationRepository.GetAll(path);

        migrations.Length.ShouldBe(5);

        migrations[0].ShouldSatisfyAllConditions(
            m => m.Name    .ShouldBe("_Begin"),
            m => m.Path    .ShouldBe(MigrationPath("_Begin")),
            m => m.Hash    .ShouldBe("265FF433D1CE5D3676A5B9B3E39F8DB299D997C7"),
            m => m.IsPseudo.ShouldBeTrue()
        );

        migrations[1].ShouldSatisfyAllConditions(
            m => m.Name    .ShouldBe("Migration0"),
            m => m.Path    .ShouldBe(MigrationPath("Migration0")),
            m => m.Hash    .ShouldBe("051B1E6D2ACFBDD6CA597BEB62D44ED3A691F7B6"),
            m => m.IsPseudo.ShouldBeFalse()
        );

        migrations[2].ShouldSatisfyAllConditions(
            m => m.Name    .ShouldBe("Migration1"),
            m => m.Path    .ShouldBe(MigrationPath("Migration1")),
            m => m.Hash    .ShouldBe("BDD667DC7336FFF32CA5460E282E5175674F690F"),
            m => m.IsPseudo.ShouldBeFalse()
        );

        migrations[3].ShouldSatisfyAllConditions(
            m => m.Name    .ShouldBe("Migration2"),
            m => m.Path    .ShouldBe(MigrationPath("Migration2")),
            m => m.Hash    .ShouldBe("48D85F49BE111A2449DA2F44C161498179ED2637"),
            m => m.IsPseudo.ShouldBeFalse()
        );

        migrations[4].ShouldSatisfyAllConditions(
            m => m.Name    .ShouldBe("_End"),
            m => m.Path    .ShouldBe(MigrationPath("_End")),
            m => m.Hash    .ShouldBe("8A3A98C3801536FEAC3C930059C2953B29F3069A"),
            m => m.IsPseudo.ShouldBeTrue()
        );
    }

    private string MigrationPath(string name)
        => Path.Combine(Path.Combine(TestDirectory, "TestDbs", "A", "Migrations", name, "_Main.sql"));

    private string TestDirectory { get; }
        = TestContext.CurrentContext.TestDirectory;
}
