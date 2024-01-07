// Copyright 2024 Subatomix Research Inc.
// SPDX-License-Identifier: ISC

namespace PSql.Deploy.Migrations;

[TestFixture]
public class MigrationSessionFactoryTests
{
    [Test]
    public void Create()
    {
        using var cancellation = new CancellationTokenSource();

        var console = Mock.Of<IMigrationConsole>();

        var session = MigrationSessionFactory.Create(console, "any", cancellation.Token);

        session.Should().Match<MigrationSession>(s
            => s.Console           == console
            && s.LogPath           == "any"
            && s.CancellationToken == cancellation.Token
        );
    }
}
