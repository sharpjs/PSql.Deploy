// Copyright Subatomix Research Inc.
// SPDX-License-Identifier: MIT

using System.Net;
using System.Security;

namespace PSql.Deploy;

[TestFixture]
[FixtureLifeCycle(LifeCycle.SingleInstance)]
public class TargetTests
{
    const string ConnectionString
        = "Data Source"     + "=sql.example.com;"
        + "Initial Catalog" + "=db;"
        + "User ID"         + "=u;"
        + "Password"        + "=p";

    [Test]
    public void Construct_NullConnectionString()
    {
        Should.Throw<ArgumentNullException>(() =>
        {
            _ = new Target(connectionString: null!);
        });
    }

    [Test]
    public void Construct_InvalidConnectionString()
    {
        Should.Throw<ArgumentException>(() =>
        {
            _ = new Target("This is not a valid connection string.");
        });
    }

    [Test]
    public void ConnectionString_Get()
    {
        new Target(ConnectionString).ConnectionString.ShouldBe(ConnectionString);
    }

    [Test]
    public void Credential_Get_Default()
    {
        new Target(ConnectionString).Credential.ShouldBeNull();
    }

    [Test]
    public void Credential_Get_Explicit([Values(false, true)] bool readOnly)
    {
        using var password = MakePassword(readOnly);

        var credential = new NetworkCredential("u", password);

        new Target(ConnectionString, credential).Credential.ShouldBeSameAs(credential);
    }

    [Test]
    public void SqlCredential_Get_Default()
    {
        new Target(ConnectionString).SqlCredential.ShouldBeNull();
    }

    [Test]
    public void SqlCredential_Get_Explicit([Values(false, true)] bool readOnly)
    {
        using var password = MakePassword(readOnly);

        var netCredential = new NetworkCredential("u", password);
        var sqlCredential = new Target(ConnectionString, netCredential).SqlCredential;

        sqlCredential.ShouldNotBeNull();
        sqlCredential.UserId                  .ShouldBe(netCredential.UserName);
        sqlCredential.Password.Apply(Unsecure).ShouldBe(netCredential.Password);
    }

    [Test]
    public void ServerDisplayName_Get_Default()
    {
        new Target("Initial Catalog=db")
            .ServerDisplayName.ShouldBe("local");
    }

    [Test]
    public void ServerDisplayName_Get_FromConnectionString()
    {
        new Target(ConnectionString)
            .ServerDisplayName.ShouldBe("sql.example.com");
    }

    [Test]
    public void ServerDisplayName_Get_Explicit()
    {
        new Target(ConnectionString, serverDisplayName: "test-server")
            .ServerDisplayName.ShouldBe("test-server");
    }

    [Test]
    public void DatabaseDisplayName_Get_Default()
    {
        new Target("Data Source=sql.example.com")
            .DatabaseDisplayName.ShouldBe("default");
    }

    [Test]
    public void DatabaseDisplayName_Get_FromConnectionString()
    {
        new Target(ConnectionString)
            .DatabaseDisplayName.ShouldBe("db");
    }

    [Test]
    public void DatabaseDisplayName_Get_Explicit()
    {
        new Target(ConnectionString, databaseDisplayName: "test-db")
            .DatabaseDisplayName.ShouldBe("test-db");
    }

    [Test]
    public void FullDisplayName_Get()
    {
        new Target(ConnectionString)
            .FullDisplayName.ShouldBe("sql.example.com.db");
    }

    private static SecureString MakePassword(bool readOnly)
    {
        var password = new SecureString();

        password.AppendChar('p');

        if (readOnly)
            password.MakeReadOnly();

        return password;
    }

    private static string Unsecure(SecureString value)
        => new NetworkCredential(null, value).Password;
}
