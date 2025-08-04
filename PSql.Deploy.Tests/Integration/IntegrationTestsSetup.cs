// Copyright Subatomix Research Inc.
// SPDX-License-Identifier: MIT

using System.Net;
using System.Net.Sockets;
using Microsoft.Data.SqlClient;

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
    private static NetworkCredential?  _credential;
    private static string?             _setupConnectionString;

    public static string SetupConnectionString
        => _setupConnectionString ?? throw OnSetUpNotExecuted();

    public static NetworkCredential? Credential
        => _credential;

    [OneTimeSetUp]
    public static async Task SetUp()
    {
        var connectionString = "Server = .";

        if (Environment.GetEnvironmentVariable(PasswordName) is { Length: > 0 } password)
        {
            // Scenario A: Environment variable MSSQL_SA_PASSWORD present.
            // => Assume that a local SQL Server default instance is running.
            //    Use the given password to authenticate as SA.
            _credential = new NetworkCredential("sa", password);
        }
        else if (IsLocalSqlServerListening())
        {
            // Scenario B: Process listening on port 1433 or named pipe.
            // => Assume that a local SQL Server default instance is running
            //    and supports integrated authentication.  Assume that the
            //    current user has suffucient privileges to run tests.
            connectionString += "; Integrated Security = true";
        }
        else
        {
            // Scenario C: No process listening on port 1433 or named pipe.
            // => Start an ephemeral SQL Server container on port 1433 using a
            //    generated SA password.
            _container  = new SqlServerContainer(ServerPort);
            _credential = _container.Credential;
        }

        connectionString = connectionString
            + "; Encrypt"          + " = false"
            + "; Application Name" + " = PSql.Deploy.Tests";

        _setupConnectionString = connectionString + "; Database = master";

        await CreateTestDatabasesAsync();
    }

    [OneTimeTearDown]
    public static async Task TearDown()
    {
        if (_container is null)
        {
            await RemoveTestDatabasesAsync();
        }
        else
        {
            // Databases will evaporate with the container
            _container.Dispose();
            _container = null;
        }
    }

    private static bool IsLocalSqlServerListening()
    {
        return OperatingSystem.IsWindows() && File.Exists(ServerPipe)
            || IsListeningOnTcpPort(ServerPort);
    }

    public static bool IsListeningOnTcpPort(ushort port)
    {
        const int TimeoutMs = 1000;

        try
        {
            using var client = new TcpClient();

            return client.ConnectAsync(IPAddress.Loopback, port).Wait(TimeoutMs)
                && client.Connected;
        }
        catch (SocketException)
        {
            return false;
        }
    }

    private static Task CreateTestDatabasesAsync(CancellationToken cancellation = default)
    {
        return Task.WhenAll(
            CreateDatabaseAsync("PSqlDeployTestA", cancellation),
            CreateDatabaseAsync("PSqlDeployTestB", cancellation)
        );
    }

    private static Task RemoveTestDatabasesAsync(CancellationToken cancellation = default)
    {
        return Task.WhenAll(
            RemoveDatabaseAsync("PSqlDeployTestA", cancellation),
            RemoveDatabaseAsync("PSqlDeployTestB", cancellation)
        );
    }

    private static Task CreateDatabaseAsync(string name, CancellationToken cancellation = default)
    {
        return ExecuteAsync(
            [GetDropDatabaseSql(name), GetCreateDatabaseSql(name)],
            cancellation
        );
    }

    private static Task RemoveDatabaseAsync(string name, CancellationToken cancellation = default)
    {
        return ExecuteAsync(
            [GetDropDatabaseSql(name)],
            cancellation
        );
    }

    private static async Task ExecuteAsync(string[] batches, CancellationToken cancellation = default)
    {
        var credential = _credential?.ToSqlCredential();

        await using var connection = new SqlConnection(SetupConnectionString, credential);
        await using var command    = connection.CreateCommand();

        command.CommandType = CommandType.Text;
        command.CommandText = "PRINT 42;";

        await connection.OpenAsync(cancellation);

        foreach (var batch in batches)
        {
            command.CommandText = batch;
            await command.ExecuteNonQueryAsync(cancellation);
        }
    }

    private static SqlCredential ToSqlCredential(this NetworkCredential credential)
    {
        var password = credential.SecurePassword;
        password.MakeReadOnly();
        return new(credential.UserName, password);
    }

    private static string GetCreateDatabaseSql(string name)
    {
        return
            $"""
            CREATE DATABASE [{name}] COLLATE Latin1_General_100_CI_AI_SC_UTF8;
            """;
    }

    private static string GetDropDatabaseSql(string name)
    {
        var nameInString        = name        .Replace("'", "''");
        var nameInQuoteInString = nameInString.Replace("]", "]]");

        return
            $"""
            IF DB_ID('{nameInString}') IS NOT NULL EXEC(N'
                ALTER DATABASE [{nameInQuoteInString}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
                DROP  DATABASE [{nameInQuoteInString}];
            ');
            """;
    }

    private static Exception OnSetUpNotExecuted()
    {
        return new InvalidOperationException(
            nameof(IntegrationTestsSetup) + "." + nameof(SetUp) + " has not executed."
        );
    }
}
