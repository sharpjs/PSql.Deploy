// Copyright Subatomix Research Inc.
// SPDX-License-Identifier: MIT

using System.Collections.Concurrent;

namespace PSql.Deploy.Integration;

[TestFixture]
public class SqlTargetConnectionIntegrationTests
{
    [Test]
    public async Task Test()
    {
        var logger = new StringSqlLogger();

        await using var connection = CreateConnection(logger);

        await connection.OpenAsync();

        await Should.ThrowAsync<DataException>(connection.GenerateMessagesAsync);

        logger.Messages.TryDequeue(out var message0).ShouldBeTrue();
        logger.Messages.TryDequeue(out var message1).ShouldBeTrue();
        logger.Messages.TryDequeue(out _           ).ShouldBeFalse();

        message0.ShouldBe       ("(batch):1: E0:0: Test message.");
        message1.ShouldStartWith("(batch):2: E8134:16: "); // ...localized divide-by-zero error message
    }

    private static TestSqlTargetConnection CreateConnection(StringSqlLogger logger)
    {
        return new(IntegrationTestsSetup.Target, logger);
    }

    private class TestSqlTargetConnection : SqlTargetConnection
    {
        public TestSqlTargetConnection(Target target, StringSqlLogger logger)
            : base(target, logger) { }

        public async Task GenerateMessagesAsync()
        {
            SetUpCommand(
                """
                PRINT 'Test message.';
                SELECT 1 / 0;
                """
            );

            await Command.ExecuteNonQueryAsync();

            ThrowIfHasErrors();
        }
    }

    private class StringSqlLogger : ISqlMessageLogger
    {
        public ConcurrentQueue<string> Messages { get; } = new();

        public void Log(string procedure, int line, int number, int severity, string? message)
        {
            message = $"{procedure}:{line}: E{number}:{severity}: {message}";

            Messages.Enqueue(message);
        }
    }
}
