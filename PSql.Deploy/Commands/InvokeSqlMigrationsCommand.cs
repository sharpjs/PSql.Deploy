// Copyright 2023 Subatomix Research Inc.
// SPDX-License-Identifier: ISC

using PSql.Deploy.Migrations;

namespace PSql.Deploy.Commands;

// TODO: Replace original Invoke-SqlMigrations with this, removing the '2' suffix
//                                           V
[Cmdlet(VerbsLifecycle.Invoke, "SqlMigrations2")]
public class InvokeSqlMigrationsCommand : Cmdlet, IMigrationLogger
{
    // -Path
    [Parameter(Mandatory = true, Position = 0)]
    [Alias("PSPath")]
    [ValidateNotNullOrEmpty]
    public string? Path { get; set; }

    // -Target
    [Parameter(Mandatory = true, Position = 1, ValueFromPipeline = true)]
    [ValidateNotNullOrEmpty]
    public SqlContextParallelSet[]? Target { get; set; }

    protected override void BeginProcessing()
    {
        using var cancellation = new CancellationTokenSource();
        using var _            = new ConsoleCancellationListener(cancellation);

        var logger = new ThreadSafeMigrationLogger(this);
        var engine = new MigrationEngine(logger, cancellation.Token);

        engine.AddMigrationsFromPath(Path!);

        // Run migrations in background threads
        Task.Run(() => RunAsync(logger, engine));

        // Use main thread to display output
        logger.Run(cancellation.Token);
    }

    private async Task RunAsync(ThreadSafeMigrationLogger logger, MigrationEngine engine)
    {
        try
        {
            await engine.RunAsync(Target!);
        }
        finally
        {
            logger.Stop();
        }
    }

    void IMigrationLogger.Log(MigrationMessage message)
    {
        WriteHost(message.ToString()!);
    }
}
