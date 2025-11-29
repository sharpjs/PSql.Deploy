// Copyright Subatomix Research Inc.
// SPDX-License-Identifier: MIT

using System.Management.Automation.Runspaces;
using System.Net;
using Subatomix.Testing.SqlServerIntegration;

namespace PSql.Deploy.Integration;

[SetUpFixture]
public static class IntegrationTestsSetup
{
    private static TemporaryDatabase? _databaseA, _databaseB;

    public static TemporaryDatabase DatabaseA
        => _databaseA ?? throw OnSetUpNotExecuted();

    public static TemporaryDatabase DatabaseB
        => _databaseB ?? throw OnSetUpNotExecuted();

    public static NetworkCredential? Credential
        => TestSqlServer.Credential;

    [OneTimeSetUp]
    public static void SetUp()
    {
        TestSqlServer.SetUp();

        _databaseA = TestSqlServer.CreateTemporaryDatabase("PSqlDeployTestA");
        _databaseB = TestSqlServer.CreateTemporaryDatabase("PSqlDeployTestB");
    }

    [OneTimeTearDown]
    public static void TearDown()
    {
        _databaseA = null;
        _databaseB = null;

        TestSqlServer.TearDown();
    }

    public static void WithIntegrationTestDefaults(InitialSessionState state)
    {
        if (!(Credential is { } c))
            return;

        state.Variables.Add(new SessionStateVariableEntry(
            "PSDefaultParameterValues",
            new DefaultParameterDictionary
            {
                ["New-SqlContext:Credential"] = new PSCredential(c.UserName, c.SecurePassword),
            },
            null
        ));
    }

    private static Exception OnSetUpNotExecuted()
    {
        return new InvalidOperationException(
            nameof(IntegrationTestsSetup) + "." + nameof(SetUp) + " has not executed."
        );
    }
}
