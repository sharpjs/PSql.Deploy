// Copyright Subatomix Research Inc.
// SPDX-License-Identifier: MIT

#pragma warning disable IDE0079 // Remove unnecessary suppression
#pragma warning disable IDE0060 // Remove unused parameter
#pragma warning disable CA1822  // Type or member can be made static

using System.Net;

namespace PSql;

public enum SqlClientVersion { Default, Mds5 }

public class TestSqlContext
{
    public string? ConnectionString { get; set; }

    public PSCredential? Credential { get; set; }

    public string GetConnectionString(
        string?          db,    // actual name in PSql: databaseName   \  To test that
        SqlClientVersion ver,   // actual name in PSql: version         > params match by
        bool             omit)  // actual name in PSql: omitCredential /  type, not name
    {
        db  .ShouldBeNull();
        ver .ShouldBe(SqlClientVersion.Mds5);
        omit.ShouldBeTrue();

        return ConnectionString!;
    }
}

public class TestAzureSqlContext : TestSqlContext
{
    public string? ServerResourceName { get; set; }
}

public class TestGetConnectionStringMissingSqlContext
{
    public NetworkCredential? Credential => throw new NotImplementedException();
}

public class TestGetConnectionStringNotPublicSqlContext
{
    public NetworkCredential? Credential => throw new NotImplementedException();

    // wrong visibility
    //vvvvvv
    internal string GetConnectionString(string? d, SqlClientVersion v, bool o)
        => throw new NotImplementedException();
}

public class TestGetConnectionStringWrongAritySqlContext
{
    public NetworkCredential? Credential => throw new NotImplementedException();

    //                                                              missing parameter
    //                                                                vvvvvvvvvvvvv
    internal string GetConnectionString(string? d, SqlClientVersion v /*, bool o */)
        => throw new NotImplementedException();
}

public class TestGetConnectionStringWrongParameterTypeSqlContext
{
    public NetworkCredential? Credential => throw new NotImplementedException();

    //                                    wrong parameter types
    //                                  vvvvvv    vvvvvv    vvvvvv
    internal string GetConnectionString(object d, object v, object o)
        => throw new NotImplementedException();
}

public class TestGetConnectionStringWrongReturnTypeSqlContext
{
    public NetworkCredential? Credential => throw new NotImplementedException();

    // wrong return type
    //     vvvv
    public Guid GetConnectionString(string? d, SqlClientVersion v, bool o)
        => throw new NotImplementedException();
}

public class TestCredentialMissingSqlContext
{
    public string GetConnectionString(string? d, SqlClientVersion v, bool o)
        => "";
}

public class TestCredentialNotReadableSqlContext
{
    //                                not readable
    //                                     vvv
    public NetworkCredential? Credential { set => throw new NotImplementedException(); }

    public string GetConnectionString(string? d, SqlClientVersion v, bool o)
        => "";
}

public class TestCredentialWrongTypeSqlContext
{
    public object Credential => "not a NetworkCredential";

    public string GetConnectionString(string? d, SqlClientVersion v, bool o)
        => "";
}

public class TestServerResourceNameNotReadableAzureSqlContext
{
    public string? ServerResourceName { set => throw new NotImplementedException(); }

    public NetworkCredential? Credential { get; set; }

    public string GetConnectionString(string? d, SqlClientVersion v, bool o)
        => "";
}

public class TestServerResourceNameWrongTypeAzureSqlContext
{
    public object? ServerResourceName => Guid.Empty;

    public NetworkCredential? Credential { get; set; }

    public string GetConnectionString(string? d, SqlClientVersion v, bool o)
        => "";
}
