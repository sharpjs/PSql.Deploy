// Copyright 2023 Subatomix Research Inc.
// SPDX-License-Identifier: ISC

namespace PSql.Deploy.Migrations;

[TestFixture]
public class MigrationSessionFactoryTests
{
    [Test]
    public void Create()
    {
        using var cancellation = new CancellationTokenSource();

        var session = MigrationSessionFactory.Create("any", cancellation.Token);

        session.Should().Match<MigrationSession>(s
            => s.LogPath           == "any"
            && s.CancellationToken == cancellation.Token
        );
    }
}
