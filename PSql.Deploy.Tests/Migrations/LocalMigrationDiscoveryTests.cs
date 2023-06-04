// Copyright 2023 Subatomix Research Inc.
// SPDX-License-Identifier: ISC

namespace PSql.Deploy.Migrations;

[TestFixture]
public class LocalMigrationDiscoveryTests
{
    [Test]
    public void GetLocalMigrations_DirectoryDoesNotExist()
    {
        var path = Path.Combine(TestDirectory, "DoesNotExist");

        var migrations = LocalMigrationDiscovery.GetLocalMigrations(path);

        migrations.Should().BeEmpty();
    }

    [Test]
    public void GetLocalMigrations_NoMigrationsInDirectory()
    {
        var path = Path.Combine(TestDirectory, "TestDbs");

        var migrations = LocalMigrationDiscovery.GetLocalMigrations(path);

        migrations.Should().BeEmpty();
    }

    [Test]
    public void GetLocalMigrations_Normal()
    {
        var path = Path.Combine(TestDirectory, "TestDbs", "A");

        var migrations = LocalMigrationDiscovery.GetLocalMigrations(path);

        migrations.Should().SatisfyRespectively(
            m =>
            {
                m.Name    .Should().Be("_Begin");
                m.Path    .Should().Be(MigrationPath("_Begin"));
                m.Hash    .Should().Be("265FF433D1CE5D3676A5B9B3E39F8DB299D997C7");
                m.IsPseudo.Should().BeTrue();
            },
            m =>
            {
                m.Name    .Should().Be("Migration0");
                m.Path    .Should().Be(MigrationPath("Migration0"));
                m.Hash    .Should().Be("051B1E6D2ACFBDD6CA597BEB62D44ED3A691F7B6");
                m.IsPseudo.Should().BeFalse();
            },
            m =>
            {
                m.Name    .Should().Be("Migration1");
                m.Path    .Should().Be(MigrationPath("Migration1"));
                m.Hash    .Should().Be("BDD667DC7336FFF32CA5460E282E5175674F690F");
                m.IsPseudo.Should().BeFalse();
            },
            m =>
            {
                m.Name    .Should().Be("Migration2");
                m.Path    .Should().Be(MigrationPath("Migration2"));
                m.Hash    .Should().Be("48D85F49BE111A2449DA2F44C161498179ED2637");
                m.IsPseudo.Should().BeFalse();
            },
            m =>
            {
                m.Name    .Should().Be("_End");
                m.Path    .Should().Be(MigrationPath("_End"));
                m.Hash    .Should().Be("8A3A98C3801536FEAC3C930059C2953B29F3069A");
                m.IsPseudo.Should().BeTrue();
            }
        );
    }

    private string MigrationPath(string name)
        => Path.Combine(Path.Combine(TestDirectory, "TestDbs", "A", "Migrations", name, "_Main.sql"));

    private string TestDirectory { get; }
        = TestContext.CurrentContext.TestDirectory;
}
