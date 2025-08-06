// Copyright Subatomix Research Inc.
// SPDX-License-Identifier: MIT

extern alias FakePSql0;
extern alias FakePSql1;

using System.Net;
using NUnit.Framework.Internal;

namespace PSql.Deploy;

[TestFixture]
[TestFixtureSource(nameof(Cases))]
[FixtureLifeCycle(LifeCycle.SingleInstance)]
public class SqlTargetDatabaseTests
{
    public static IEnumerable<object[]> Cases => [[false], [true]];

    private readonly Func<object, object> _wrap;

    public SqlTargetDatabaseTests(bool wrapped)
    {
        _wrap = wrapped
            ? obj => new PSObject(obj)
            : obj => obj;
    }

    [Test]
    public void Construct_WithValues()
    {
        var credential = MakePSCredential("sa", "secret");

        var target = new SqlTargetDatabase(
            "Server = localhost; Database = Test",
            credential,
            "My Server",
            "My Database"
        );

        target.ConnectionString   .ShouldBe("Server = localhost; Database = Test");
        target.Credential         .ShouldBeSameAs(credential);
        target.ServerDisplayName  .ShouldBe("My Server");
        target.DatabaseDisplayName.ShouldBe("My Database");
    }

    [Test]
    public void Construct_WithValues_Defaults()
    {
        var target = new SqlTargetDatabase("Server = localhost; Database = Test");

        target.ConnectionString   .ShouldBe("Server = localhost; Database = Test");
        target.Credential         .ShouldBeNull();
        target.ServerDisplayName  .ShouldBe("localhost");
        target.DatabaseDisplayName.ShouldBe("Test");
    }

    [Test]
    public void Construct_WithValues_NullConnectionString()
    {
        Should.Throw<ArgumentNullException>(() =>
        {
            _ = new SqlTargetDatabase(connectionString: null!);
        });
    }

    [Test]
    public void Construct_FromNull()
    {
        Should.Throw<ArgumentNullException>(() =>
        {
            _ = new SqlTargetDatabase(obj: null!);
        });
    }

    [Test]
    public void Construct_FromSameType()
    {
        var source = new SqlTargetDatabase(
            "Server = localhost; Database = Test",
            MakePSCredential("sa", "secret"),
            "My Server",
            "My Database"
        );

        var actual = MakeSqlTargetDatabase(source);

        actual.ShouldBeEquivalentTo(source);
    }

    [Test]
    public void Construct_FromConnectionString()
    {
        var source = "Server = localhost; Database = Test; User ID = sa; Password = secret";

        var actual = MakeSqlTargetDatabase(source);

        actual.ConnectionString   .ShouldBe(source);
        actual.Credential         .ShouldBeNull();
        actual.ServerDisplayName  .ShouldBe("localhost");
        actual.DatabaseDisplayName.ShouldBe("Test");
    }

    [Test]
    public void Construct_FromSqlContext()
    {
        var source = new TestSqlContext
        {
            ConnectionString = "Server = localhost; Database = Test",
            Credential       = MakePSCredential("sa", "secret"),
        };

        var actual = MakeSqlTargetDatabase(source);

        actual.ConnectionString   .ShouldBe(source.ConnectionString);
        actual.Credential         .ShouldBe(source.Credential);
        actual.ServerDisplayName  .ShouldBe("localhost");
        actual.DatabaseDisplayName.ShouldBe("Test");
    }

    [Test]
    public void Construct_FromSqlContext_CredentialIsNull()
    {
        var source = new TestSqlContext
        {
            ConnectionString = "Server = localhost; Database = Test"
        };

        var actual = MakeSqlTargetDatabase(source);

        actual.ConnectionString   .ShouldBe(source.ConnectionString);
        actual.Credential         .ShouldBeNull();
        actual.ServerDisplayName  .ShouldBe("localhost");
        actual.DatabaseDisplayName.ShouldBe("Test");
    }

    [Test]
    public void Construct_FromAzureSqlContext()
    {
        var source = new TestAzureSqlContext
        {
            ServerResourceName = "example",
            ConnectionString   = "Server = example.database.windows.net; Database = Test",
            Credential         = MakePSCredential("sa", "secret"),
        };

        var actual = MakeSqlTargetDatabase(source);

        actual.ConnectionString   .ShouldBe(source.ConnectionString);
        actual.Credential         .ShouldBe(source.Credential);
        actual.ServerDisplayName  .ShouldBe("example");
        actual.DatabaseDisplayName.ShouldBe("Test");
    }

    [Test]
    public void Construct_FromAzureSqlContext_ServerResourceNameIsNull()
    {
        var source = new TestAzureSqlContext
        {
            ConnectionString   = "Server = example.database.windows.net; Database = Test",
            Credential         = MakePSCredential("sa", "secret"),
        };

        var actual = MakeSqlTargetDatabase(source);

        actual.ConnectionString   .ShouldBe(source.ConnectionString);
        actual.Credential         .ShouldBe(source.Credential);
        actual.ServerDisplayName  .ShouldBe("example.database.windows.net");
        actual.DatabaseDisplayName.ShouldBe("Test");
    }

    [Test]
    public void Construct_FromSqlContext_SqlClientVersionEnumMissing()
    {
        ShouldThrowDueToNonConformingApi(
            new FakePSql0::PSql.SqlContext()
        );
    }

    [Test]
    public void Construct_FromSqlContext_SqlClientVersionEnumMemberMissing()
    {
        ShouldThrowDueToNonConformingApi(
            new FakePSql1::PSql.SqlContext()
        );
    }

    [Test]
    public void Construct_FromSqlContext_GetConnectionStringMissing()
    {
        ShouldThrowDueToNonConformingApi(
            new TestGetConnectionStringMissingSqlContext()
        );
    }

    [Test]
    public void Construct_FromSqlContext_GetConnectionStringWrongArity()
    {
        ShouldThrowDueToNonConformingApi(
            new TestGetConnectionStringWrongAritySqlContext()
        );
    }

    [Test]
    public void Construct_FromSqlContext_GetConnectionStringWrongParameterType()
    {
        ShouldThrowDueToNonConformingApi(
            new TestGetConnectionStringWrongParameterTypeSqlContext()
        );
    }

    [Test]
    public void Construct_FromSqlContext_GetConnectionStringWrongReturnType()
    {
        ShouldThrowDueToNonConformingApi(
            new TestGetConnectionStringWrongReturnTypeSqlContext()
        );
    }

    [Test]
    public void Construct_FromSqlContext_GetConnectionStringReturnsNull()
    {
        ShouldThrowDueToNonConformingApi(
            new TestSqlContext
            {
                ConnectionString = null,
                Credential       = MakePSCredential("sa", "secret"),
            }
        );
    }

    [Test]
    public void Construct_FromSqlContext_CredentialMissing()
    {
        ShouldThrowDueToNonConformingApi(
            new TestCredentialMissingSqlContext()
        );
    }

    [Test]
    public void Construct_FromSqlContext_CredentialNotReadable()
    {
        ShouldThrowDueToNonConformingApi(
            new TestCredentialNotReadableSqlContext()
        );
    }

    [Test]
    public void Construct_FromAzureSqlContext_CredentialWrongType()
    {
        ShouldThrowDueToNonConformingApi(
            new TestCredentialWrongTypeSqlContext()
        );
    }

    [Test]
    public void Construct_FromAzureSqlContext_ServerResourceNameNotReadable()
    {
        ShouldThrowDueToNonConformingApi(
            new TestServerResourceNameNotReadableAzureSqlContext()
        );
    }

    [Test]
    public void Construct_FromSqlContext_ServerResourceNameWrongType()
    {
        ShouldThrowDueToNonConformingApi(
            new TestServerResourceNameWrongTypeAzureSqlContext()
        );
    }

    [Test]
    public void Construct_FromOther()
    {
        Should
            .Throw<ArgumentException>(() => MakeSqlTargetDatabase(new object()))
            .Message.ShouldContain("Unsupported conversion.");
    }

    private void ShouldThrowDueToNonConformingApi(object obj)
    {
        Should
            .Throw<ArgumentException>(() => MakeSqlTargetDatabase(obj))
            .Message.ShouldBe("The object does not conform to the expected API surface of PSql.SqlContext.");
    }

    private SqlTargetDatabase MakeSqlTargetDatabase(object obj)
        => new SqlTargetDatabase(_wrap(obj));

    private PSCredential MakePSCredential(string userName, string password)
    {
        var networkCredential = new NetworkCredential(userName, password);

        return new(
            networkCredential.UserName,
            networkCredential.SecurePassword
        );
    }
}
