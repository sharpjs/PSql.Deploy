// Copyright Subatomix Research Inc.
// SPDX-License-Identifier: MIT

using System.Net;

namespace PSql.Deploy.Integration;

[SetUpFixture]
public static class IntegrationTestsSetup
{
    private const string
        PasswordName = "MSSQL_SA_PASSWORD",
        ServerPipe   = @"\\.\pipe\sql\query";

    private const ushort
        ServerPort = 1433;

    private static SqlServerContainer? _container;
    private static Target?             _setupTarget;
    private static Target?             _testTarget;

    internal static Target Target
        => _testTarget ?? throw new InvalidOperationException("Target not initialized.");

    [OneTimeSetUp]
    public static async Task SetUp()
    {
        var connectionString = new SqlConnectionStringBuilder { DataSource = "." };
        var credential       = null as NetworkCredential;

        var password = Environment
            .GetEnvironmentVariable(PasswordName)
            .NullIfEmpty();

        if (password is not null)
        {
            // Scenario A: Environment variable MSSQL_SA_PASSWORD present.
            // => Assume that a local SQL Server default instance is running.
            //    Use the given password to authenticate as SA.
            credential = new NetworkCredential("sa", password);
        }
        else if (IsLocalSqlServerListening())
        {
            // Scenario B: Process listening on port 1433 or named pipe.
            // => Assume that a local SQL Server default instance is running
            //    and supports integrated authentication.  Assume that the
            //    current user has suffucient privileges to run tests.
            connectionString.IntegratedSecurity = true;
        }
        else
        {
            // Scenario C: No process listening on port 1433 or named pipe.
            // => Start an ephemeral SQL Server container on port 1433 using a
            //    generated SA password.
            _container = new SqlServerContainer(ServerPort);
            credential = _container.Credential;
        }

        connectionString.Encrypt         = SqlConnectionEncryptOption.Optional;
        connectionString.ApplicationName = "PSql.Deploy.Tests";

        _setupTarget = new(connectionString.ToString(), credential);

        connectionString.InitialCatalog = "PSqlDeployTest";

        _testTarget = new(connectionString.ToString(), credential);

        await CreateTestDatabaseAsync();
    }


    private static bool IsLocalSqlServerListening()
    {
        return TcpPort.IsListening(ServerPort)
            || OperatingSystem.IsWindows() && File.Exists(ServerPipe);
    }

    [OneTimeTearDown]
    public static async Task TearDown()
    {
        try
        {
            await RemoveTestDatabaseAsync();
        }
        finally
        {
            _container?.Dispose();
            _container = null;
        }
    }

    private static async Task CreateTestDatabaseAsync()
    {
        if (_setupTarget is null)
            return;

        await using var connection = new SqlTestTargetConnection(_setupTarget);

        await connection.OpenAsync(CancellationToken.None);
        await connection.RemoveDatabaseAsync("PSqlDeployTest");
        await connection.CreateDatabaseAsync("PSqlDeployTest");
    }

    private static async Task RemoveTestDatabaseAsync()
    {
        if (_setupTarget is null)
            return;

        await using var connection = new SqlTestTargetConnection(_setupTarget);

        await connection.OpenAsync(CancellationToken.None);
        await connection.RemoveDatabaseAsync("PSqlDeployTest");
    }
}
