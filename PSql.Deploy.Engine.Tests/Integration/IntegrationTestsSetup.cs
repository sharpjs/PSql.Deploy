// Copyright Subatomix Research Inc.
// SPDX-License-Identifier: MIT

using Subatomix.Testing.SqlServerIntegration;

namespace PSql.Deploy.Integration;

[SetUpFixture]
public static class IntegrationTestsSetup
{
    private static TemporaryDatabase? _database;
    private static Target?            _target;

    internal static Target Target
        => _target ?? throw OnSetUpNotExecuted();

    [OneTimeSetUp]
    public static void SetUp()
    {
        TestSqlServer.SetUp();

        _database = TestSqlServer.CreateTemporaryDatabase("PSqlDeployTest");
        _target   = new(_database.ConnectionString, TestSqlServer.Credential);
    }


    [OneTimeTearDown]
    public static void TearDown()
    {
        _database = null;
        _target   = null;

        TestSqlServer.TearDown();
    }

    private static Exception OnSetUpNotExecuted()
    {
        return new InvalidOperationException(
            nameof(IntegrationTestsSetup) + "." + nameof(SetUp) + " has not executed."
        );
    }
}
