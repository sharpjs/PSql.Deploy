// Copyright Subatomix Research Inc.
// SPDX-License-Identifier: MIT

namespace PSql.Deploy;

using static SqlMessageConstants;

/// <inheritdoc cref="ITargetConnection"/>
internal abstract class SqlTargetConnection : ITargetConnection
{
    /// <summary>
    ///   Initializes a new <see cref="SqlTargetConnection"/> instance.
    /// </summary>
    /// <param name="target">
    ///   An object representing the target database.
    /// </param>
    /// <param name="logger">
    ///   The logger for server messages received over the connection.
    /// </param>
    /// <exception cref="ArgumentNullException">
    ///   <paramref name="target"/> and/or
    ///   <paramref name="logger"/> is <see langword="null"/>.
    /// </exception>
    protected SqlTargetConnection(Target target, ISqlMessageLogger logger)
    {
        if (target is null)
            throw new ArgumentNullException(nameof(target));
        if (logger is null)
            throw new ArgumentNullException(nameof(logger));

        Target = target;
        Logger = logger;

        Connection = target.SqlCredential is { } credential
            ? new(target.ConnectionString, credential)
            : new(target.ConnectionString);

        Connection.RetryLogicProvider                = RetryLogicProvider;
        Connection.FireInfoMessageEventOnUserErrors  = true;
        Connection.InfoMessage                      += HandleMessage;
        Connection.Disposed                         += HandleUnexpectedDisposal;

        Command                    = Connection.CreateCommand();
        Command.CommandType        = CommandType.Text;
        Command.RetryLogicProvider = RetryLogicProvider;
    }

    /// <summary>
    ///   Gets the retry logic for connections.
    /// </summary>
    protected static SqlRetryLogicBaseProvider RetryLogicProvider { get; }
        = SqlConfigurableRetryFactory.CreateExponentialRetryProvider(new()
        {
            NumberOfTries   = 5,
            DeltaTime       = TimeSpan.FromSeconds(2),
            MaxTimeInterval = TimeSpan.FromMinutes(2),
            TransientErrors = TransientErrorNumbers,
        });

    /// <inheritdoc/>
    public Target Target { get; }

    /// <summary>
    ///   Gets the underlying SqlClient connection.
    /// </summary>
    protected SqlConnection Connection { get; }

    /// <summary>
    ///   Gets the underlying SqlClient command.
    /// </summary>
    protected SqlCommand Command { get; }

    /// <inheritdoc/>
    public ISqlMessageLogger Logger { get; }

    /// <summary>
    ///   Gets whether one or more error messages have been received over the
    ///   connection since the most recent invocation of
    ///   <see cref="ClearErrors"/>.
    /// </summary>
    protected bool HasErrors { get; private set; }

    /// <summary>
    ///   Throws an exception if one or more error messages have been received
    ///   over the connection since the most recent invocation of
    ///   <see cref="ClearErrors"/>.
    /// </summary>
    /// <exception cref="DataException">
    ///   One or more error messages have been received over the connection
    ///   since the most recent invocation of <see cref="ClearErrors"/>.
    /// </exception>
    protected void ThrowIfHasErrors()
    {
        if (HasErrors)
            throw new DataException("An error occurred while executing the SQL batch.");
    }

    /// <summary>
    ///   Resets error-handling state, forgetting any error messages that have
    ///   been received over the connection.
    /// </summary>
    protected void ClearErrors()
    {
        HasErrors = false;
    }

    /// <summary>
    ///   Configures the <see cref="Command"/> object.
    /// </summary>
    /// <param name="sql">
    ///   The command text to execute.
    /// </param>
    /// <param name="timeout">
    ///   The command timeout, in seconds, or <c>0</c> for no timeout.
    /// </param>
    /// <param name="parameters">
    ///   The parameters for the command, specified as name-value pairs.
    ///   This method interprets <see langword="null"/> as <c>DBNull.Value</c>.
    /// </param>
    /// <returns>
    ///   The value of the <see cref="Command"/> property, configured with the
    ///   specified <paramref name="sql"/>, <paramref name="timeout"/>, and
    ///   <paramref name="parameters"/>.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    ///   <paramref name="sql"/> is <see langword="null"/>.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    ///   <paramref name="timeout"/> is less than <c>0</c>.
    /// </exception>
    protected SqlCommand SetUpCommand(
        string                     sql,
        int                        timeout = 0,
        params (string, object?)[] parameters)
    {
        ArgumentNullException.ThrowIfNull(sql);

        if (timeout < 0)
            throw new ArgumentOutOfRangeException(nameof(timeout));

        ClearErrors();

        Command.CommandText    = sql;
        Command.CommandTimeout = timeout;

        Command.Parameters.Clear();

        foreach (var (name, value) in parameters)
            Command.Parameters.AddWithValue(name, value ?? DBNull.Value);

        return Command;
    }

    /// <inheritdoc/>
    public Task OpenAsync(CancellationToken cancellation = default)
    {
        return Connection.OpenAsync(cancellation);
    }

    /// <inheritdoc/>
    public virtual void Dispose()
    {
        // Disposal is now expected
        Connection.Disposed -= HandleUnexpectedDisposal;

        Command   .Dispose();
        Connection.Dispose();

        GC.SuppressFinalize(this);
    }

    /// <inheritdoc/>
    public virtual async ValueTask DisposeAsync()
    {
        // Disposal is now expected
        Connection.Disposed -= HandleUnexpectedDisposal;

        await Command   .DisposeAsync();
        await Connection.DisposeAsync();

        GC.SuppressFinalize(this);
    }

    private void HandleMessage(object sender, SqlInfoMessageEventArgs e)
    {
        const string NonProcedureLocationName = "(batch)";

        foreach (SqlError error in e.Errors)
        {
            Assume.NotNull(error); // SqlClient code appears to assume this

            // Mark current command if failed
            HasErrors |= error.Class > MaxInformationalSeverity;

            Logger.Log(
                error.Procedure.NullIfEmpty() ?? NonProcedureLocationName,
                error.LineNumber,
                error.Number,
                error.Class,
                error.Message
            );
        }
    }

    [DoesNotReturn]
    private static void HandleUnexpectedDisposal(object? sender, EventArgs e)
    {
        throw new DataException(
            "The connection to the database server was closed unexpectedly."
        );
    }

    private static readonly HashSet<int> TransientErrorNumbers = new()
    {
        // Sources:
        //
        // * EF Core:
        //   https://github.com/dotnet/efcore/blob/v9.0.10/src/EFCore.SqlServer/Storage/Internal/SqlServerTransientExceptionDetector.cs
        //   The primary source for this list.  Appears quite comprehensive.
        //   The list is ordered from greatest to least for some unknown
        //   reason; that ordering is preserved here.
        //
        // * SqlClient:
        //   https://github.com/dotnet/SqlClient/blob/main/src/Microsoft.Data.SqlClient/src/Microsoft/Data/SqlClient/Reliability/SqlConfigurableRetryFactory.cs
        //   A more conservative list, but does have one unique entry (4221),
        //   marked with '//*' below.
        //
        // * Proprietary:
        //   An unnamed proprietary source contains one unique entry (40545),
        //   marked with '//=' below.

        49983, // The '%ls' operation failed to complete. Retry the operation. Create a support request if the retry attempts do not succeed.
        49977, // The elastic pool '%ls' is busy with another operation. Please wait until the ongoing operation finishes and try again.
        49920, // Cannot process request. Too many operations in progress for subscription "%ld". The service is busy processing multiple requests for this subscription. Requests are currently blocked for resource optimization. Query sys.dm_operation_status for operation status. Wait until pending requests are complete or delete one of your pending requests and retry your request later.
        49919, // Cannot process create or update request. Too many create or update operations in progress for subscription "%ld". The service is busy processing multiple create or update requests for your subscription or server. Requests are currently blocked for resource optimization. Query sys.dm_operation_status for pending operations. Wait till pending create or update requests are complete or delete one of your pending requests and retry your request later.
        49918, // Cannot process request. Not enough resources to process request. The service is currently busy. Please retry the request later.
        49802, // Database is unavailable at the moment, please retry connection at later time.
        49510, // Managed instance is busy with another operation. Please try your operation later.
        47139, // Join contained availability group '%.*ls' failed to create group master '%.*ls' Database ID. Please retry the operation again.
        47137, // Cannot create contained system databases in contained availability group '%.*ls' It might be caused by temporary condition. Retry the operation.
        47132, // Joining availability group '%.*ls' with rebuilding contained system DB has failed because rebuilding contained MSDB has failed. This is caused by contained MSDB is still used. Retry the operation later.
        45547, // The create operation has timed out in one of the backend workflows. Please retry the operation.
        45319, // The service objective assignment for database '%.*ls' on server '%.*ls' could not be completed as the database is too busy. Reduce the workload before initiating another service objective update.
        45182, // Database '%ls' is busy with another operation. Please try your operation later.
        45179, // The operation timed out and automatically rolled back. Please retry the operation.
        45161, // Managed instance '%.*ls' is busy with another operation. Please try your operation later.
        45157, // Server '%.*ls' is busy with another operation. Please try your operation later.
        45156, // Subscription '%.*ls' is busy with another operation. Please try your operation later.
        45153, // Management Service is not currently available. Please retry the operation later. If the problem persists, contact customer support, and provide them the session tracing ID of '%ls'.
        42109, // The SQL pool is warming up. Please try again.
        42108, // Can not connect to the SQL pool since it is paused. Please resume the SQL pool and try again.
        42029, // An internal error happened while generating a new DBTS for database %.*ls. Please retry the operation.
        41840, // Could not perform the operation because the elastic pool or managed instance has reached its quota for in-memory tables. This error may be transient. Please retry the operation. See 'http://go.microsoft.com/fwlink/?LinkID=623028' for more information.
        41839, // Transaction exceeded the maximum number of commit dependencies.
        41823, // Could not perform the operation because the database has reached its quota for in-memory tables. This error may be transient. Please retry the operation. See 'http://go.microsoft.com/fwlink/?LinkID=623028' for more information.
        41701, // The Activation Context is unavailable at this time. The Windows Fabric Runtime is unavailable at this time, retry later. Wait for the activation context to become available, then retry.
        41700, // System views related to Windows Fabric partitions and replicas are not available at this time, because replica manager has not yet started. Wait for replica manager to start, then retry the system view query.
        41640, // Database '%ls' encountered a transient error (error code: 0x%08X) while performing task '%ls'. Refer to the SQL Server error log for information about the errors that were encountered. If this condition persists, contact the system administrator.
        41619, // Windows Fabric '%ls' (partition ID '%ls')encountered transient error %d while waiting for build replica operation on database '%ls' (ID %d). Refer to the SQL Server error log for information about the errors that were encountered. If this condition persists, contact the system administrator.
        41614, // Fabric Service '%ls' encountered a transient error while performing Windows Fabric operation on '%ls' database (ID %d). Refer to the SQL Server error log for information about the errors that were encountered. If this condition persists, contact the system administrator.
        41383, // An internal error occurred while running the DMV query. This was likely caused by concurrent DDL operations. Please retry the query.
        41339, // The table '%.*ls' has been created or altered after the start of the current transaction. The transaction was aborted. Please retry the transaction.
        41325, // The current transaction failed to commit due to a serializable validation failure.
        41305, // The current transaction failed to commit due to a repeatable read validation failure.
        41302, // The current transaction attempted to update a record that has been updated since the transaction started.
        41301, // Dependency failure: a dependency was taken on another transaction that later failed to commit.
        40938, // The Server DNS Alias '%.*ls' is busy with another operation and cannot perform the '%.*ls' operation. Please try again later.
        40918, // The Failover Group '%.*ls' is busy with another operation and cannot perform the '%.*ls' operation. Please try again later
        40903, // The server '%.*ls' is currently busy. Please wait a few minutes before trying again.
        40890, // The elastic pool is busy with another operation.
        40675, // The service is currently too busy. Please try again later.
        40671, // Unable to '%.*ls' '%.*ls' on server '%.*ls'. Please retry the connection later.
        40648, // Too many requests have been performed. Please retry later.
        40642, // The server is currently too busy. Please try again later.
        40623, // Reauthentication failed for login %.*ls. Within the past reauthentification interval, the login has become invalid due to a password change, a dropped login, or other cause. Please retry login.
        40613, // Database XXXX on server YYYY is not currently available. Please retry the connection later. If the problem persists, contact customer support, and provide them the session tracing ID of ZZZZZ.
        40545, //=The service is experiencing a problem that is currently under investigation. Incident ID: %ls. Code: %d.
        40540, // The service has encountered an error processing your request. Please try again.
        40501, // The service is currently busy. Retry the request after 10 seconds. Code: (reason code to be decoded).
        40197, // The service has encountered an error processing your request. Please try again.
        40189, // The resource quota for the current database has been exceeded and this request has been aborted. Please rerun your request in the next quota window. %s
        40143, // The service has encountered an error processing your request. Please try again.
        40106, // The schema scope set in the session is not the current schema scope for the current partition. Please rerun your query.
        39152, // Transient error communicating with the streaming runtime due to unfinished stop streaming job operation. Please retry the operation.
        39151, // Transient error communicating with the streaming runtime. Please retry the operation.
        39110, // Maximum number of concurrent external script queries for this user has been reached. Limit is %d. Please retry the operation. External script request id is %ls.
        39108, // Maximum number of concurrent external script users has been reached. Limit is %d. Please retry the operation. External script request id is %ls.
        39025, // External script execution failed as extensibility environment is not ready yet. Retry the operation when the server is fully started.
        37327, // Maximum number of concurrent DBCC commands running in the enclave has been reached. The maximum number of concurrent DBCC queries is %d. Try rerunning the query.
        37202, // An instance pool with name '%.*ls' is busy with another ongoing operation.
        35293, // Error in retrieving extended recovery forks from the primary replica. The extended-recovery-fork stack changed while being retrieved by the secondary replica. Retry the operation.
        35256, // The session timeout value was exceeded while waiting for a response from the other availability replica in the session. That replica or the network might be down, or the command might be misconfigured. If the partner is running and visible over the network, retry the command using correctly configured partner-connection parameters.
        35218, // An error occurred while trying to set the initial Backup LSN of database '%.*ls'. Primary database is temporarily offline due to restart or other transient condition. Retry the operation.
        35216, // An error occurred while adding or removing a log truncation holdup to build secondary replica from primary availability database '%.*ls'. Primary database is temporarily offline due to restart or other transient condition. Retry the operation.
        33123, // Cannot drop or alter the database encryption key since it is currently in use on a mirror or secondary availability replica. Retry the command after all the previous reencryption scans have propagated to the mirror or secondary availability replicas or after availability relationship has been disabled.
        30085, // A stoplist cache cannot be generated while processing a full-text query or performing full-text indexing. There is not enough memory to load the stoplist cache. Rerun the query or indexing command when more resources are available.
        30080, // The full-text population on table '%ls' cannot be started because the full-text catalog is importing data from existing catalogs. After the import operation finishes, rerun the command.
        25740, // Unable to start event session '%.*ls' because system is busy. Please try again later.
        25738, // Event session '%.*ls' could not be started because system is currently busy. Please try again later.
        25003, // Upgrade of the distribution database MSmerge_subscriptions table failed. Rerun the upgrade procedure in order to upgrade the distribution database.
        22984, // An error occurred while waiting on the log reader history cache event. This error is reported by the internal task scheduling and might be transient. Retry the operation.
        22760, // Aborting Synapse Link Publish task for partition %ld timed out. Retry this operation later.
        22759, // Aborting Synapse Link Snapshot task for table %ld timed out. Retry this operation later.
        22758, // Aborting Synapse Link Commit task for table group '%s' timed out. Retry this operation later.
        22754, // Aborting Synapse Link Capture task for this database timed out. Retry this operation later.
        22498, // There is not enough resources to perform the operation. Please retry your operation later.
        22493, // The database '%.*ls' failed to sync. Please retry the operation again. If the problem persists contact Microsoft Azure Customer Support.
        22491, // The DDL statement failed due to an internal error. Please retry the operation again. If the problem persists contact Microsoft Azure Customer Support.
        22430, // Operation failed as the Database '%.*ls' is shutting down. Please retry the operation again. If the problem persists contact Microsoft Azure Customer Support.
        22427, // Operation failed due to an error in a background task. Please retry the operation again. If the problem persists contact Microsoft Azure Customer Support.
        22358, // The Database Controller required for this operation was not found. Please retry the operation again. If the problem persists contact Microsoft Azure Customer Support.
        22355, // Lock manager does not exist. Please retry the operation again. If the problem persists contact Microsoft Azure Customer Support.
        22353, // The SQL instances has not been correctly setup to allow this operation. Please retry the operation again. If the problem persists contact Microsoft Azure Customer Support.
        22335, // Cannot obtain a LOCK resource at this time due to internal error. Rerun your statement when there are fewer active users.
        22226, // An internal error (%d, %d) occured. Please retry the operation again. If the problem persists contact Microsoft Azure Customer Support.
        22225, // An internal error (%d, %d) occured. Please retry the operation again. If the problem persists contact Microsoft Azure Customer Support.
        21503, // Cleanup of merge meta data cannot be performed while merge processes are running. Retry this operation after the merge processes have completed.
        20041, // Transaction rolled back. Could not execute trigger. Retry your transaction.
        19494, // Automatic seeding of availability database '%ls' in availability group '%.*ls' failed with a transient error. The operation will be retried.
        19416, // One or more databases in availability group '%.*ls' are not synchronized. On a synchronous-commit availability replica, ALTER AVAILABILITY GROUP <group_name> OFFLINE is not allowed when one or more databases are not synchronized. Wait for all databases to reach the SYNCHRONIZED state, and retry the command.
        19413, // An attempt to switch Always On Availability Groups to the local Windows Server Failover Clustering (WSFC) cluster context failed. This attempt failed because switching the cluster context back to the local cluster at this time might cause data loss because one or more secondary databases on synchronous-commit replicas are not in the SYNCHRONIZED state. Wait until all synchronous-commit secondary databases are synchronized, and then retry the ALTER SERVER CONFIGURATION SET HADR CLUSTER LOCAL command.
        18858, // Could not read data from replication table '%s'. If retrying does not fix the issue, drop and reconfigure replication.
        18401, // Login failed for user '%s'. Reason: Server is in script upgrade mode. Only administrator can connect at this time.
        17889, // A new connection was rejected because the maximum number of connections on session ID %d has been reached. Close an existing connection on this session and retry.%.*ls
        17197, // Login failed due to timeout; the connection has been closed. This error may indicate heavy server load. Reduce the load on the server and retry login.
        17067, // SQL Server Assertion: File: <%s>, line = %d %s. This error may be timing-related. If the error persists after rerunning the statement, use DBCC CHECKDB to check the database for structural integrity, or restart the server to ensure in-memory data structures are not corrupted.
        17066, // SQL Server Assertion: File: <%s>, line=%d Failed Assertion = '%s'. This error may be timing-related. If the error persists after rerunning the statement, use DBCC CHECKDB to check the database for structural integrity, or restart the server to ensure in-memory data structures are not corrupted.
        17065, // SQL Server Assertion: File: <%s>, line = %d Failed Assertion = '%s' %s. This error may be timing-related. If the error persists after rerunning the statement, use DBCC CHECKDB to check the database for structural integrity, or restart the server to ensure in-memory data structures are not corrupted.
        16555, // Operation failed due to an error while processing rejected rows. Intermediate results, if any, should be discarded as such results may not be complete. Please retry the operation. If the problem persists, contact Microsoft Azure Customer Support.
        16554, // Operation failed due to an error in a background task. Please retry the operation again. If the problem persists contact Microsoft Azure Customer Support.
        16528, // Operation '%ls %ls' failed. Retry the operation later.
        14906, // The database '%s' is not accessible. Ensure that the remote database exists. If you believe that your database should be accessible please retry the command.
        14868, // Inbound migration is in progress or paused. Migration direction outbound cannot be set at this time. Please retry after inbound migration is complete.
        14817, // The server '%s' is not accessible. Ensure that the remote server exists and the Azure SQL DB Firewall Rules permit access to the server. If you believe that your server should be accessible please retry the command.
        14355, // The MSSQLServerADHelper service is busy. Retry this operation later.
        11539, // One of the types specified in WITH RESULT SETS clause has been modified after the EXECUTE statement started running. Please rerun the statement.
        11001, // An error has occurred while establishing a connection to the server. When connecting to SQL Server, this failure may be caused by the fact that under the default settings SQL Server does not allow remote connections. (provider: TCP Provider, error: 0 - No such host is known.) (.Net SqlClient Data Provider)
        10936, // Resource ID : %d. The request limit for the elastic pool is %d and has been reached. See 'https://go.microsoft.com/fwlink/?LinkId=267637' for assistance.
        10930, // The service is currently too busy. Please try again later.
        10929, // Resource ID: %d. The %s minimum guarantee is %d, maximum limit is %d and the current usage for the database is %d. However, the server is currently too busy to support requests greater than %d for this database. For more information, see https://go.microsoft.com/fwlink/?LinkId=267637. Otherwise, please try again.
        10928, // Resource ID: %d. The %s limit for the database is %d and has been reached. For more information, see https://go.microsoft.com/fwlink/?LinkId=267637.
        10922, // %ls failed. Rerun the statement.
        10060, // A network-related or instance-specific error occurred while establishing a connection to SQL Server. The server was not found or was not accessible. Verify that the instance name is correct and that SQL Server is configured to allow remote connections. (provider: TCP Provider, error: 0 - A connection attempt failed because the connected party did not properly respond after a period of time, or established connection failed because connected host has failed to respond.)"}
        10054, // A transport-level error has occurred when sending the request to the server. (provider: TCP Provider, error: 0 - An existing connection was forcibly closed by the remote host.)
        10053, // A transport-level error has occurred when receiving results from the server. An established connection was aborted by the software in your host machine.
         9985, // There is not enough memory to generate a search property list cache. Rerun your full-text indexing statement when more resources are available.
         9515, // An XML schema has been altered or dropped, and the query plan is no longer valid. Please rerun the query batch.
         9020, // The log for database '%ls' failed to grow while shrink in progress. Please retry.
         8651, // Could not perform the operation because the requested memory grant was not available in resource pool '%ls' (%ld). Rerun the query, reduce the query load, or check resource governor configuration setting.
         8645, // A timeout occurred while waiting for memory resources to execute the query in resource pool '%ls' (%ld). Rerun the query.
         8628, // A time out occurred while waiting to optimize the query. Rerun the query.
         7951, // Warning: Could not complete filestream consistency checks due to an operating system error. Any consistency errors found in the filestream subsystem will be silenced. Please refer to other errors for more information. This condition is likely transient; try rerunning the command.
         6292, // The transaction that is associated with this operation has been committed or rolled back. Retry with a different transaction.
         5529, // Failed to remove a FILESTREAM file. The database is a primary database in an availability group. Wait for the FILESTREAM data files to be hardened on every secondary availability replica. Then retry the drop file operation.
         5280, // An unexpected protocol element was recevied during the execution of a consistency check command. Retry the operation.
         4221, // Login to read-secondary failed due to long wait on 'HADR_DATABASE_WAIT_FOR_TRANSITION_TO_VERSIONING'. The replica is not available for login because row versions are missing for transactions that were in-flight when the replica was recycled. The issue can be resolved by rolling back or committing the active transactions on the primary replica. Occurrences of this condition can be minimized by avoiding long write transactions on the primary.
         3980, // The request failed to run because the batch is aborted, this can be caused by abort signal sent from client, or another request is running in the same session, which makes the session busy.
         4221, //*Login to read-secondary failed due to long wait on 'HADR_DATABASE_WAIT_FOR_TRANSITION_TO_VERSIONING'. The replica is not available for login because row versions are missing for transactions that were in-flight when the replica was recycled. The issue can be resolved by rolling back or committing the active transactions on the primary replica. Occurrences of this condition can be minimized by avoiding long write transactions on the primary.
         4184, // Cannot retrieve table data for the query operation because the table ""%.*ls"" schema is being altered too frequently. Because the table ""%.*ls"" contains a filtered index or filtered statistics, changes to the table schema require a refresh of all table data. Retry the query operation, and if the problem persists, use SQL Server Profiler to identify what schema-altering operations are occurring.
         4117, // Cannot retrieve table data for the query operation because the table ""%.*ls"" schema is being altered too frequently. Because the table ""%.*ls"" contains a computed column, changes to the table schema require a refresh of all table data. Retry the query operation, and if the problem persists, use SQL Server Profiler to identify what schema-altering operations are occurring.
         4060, // Cannot open database "%.*ls" requested by the login. The login failed.
         3966, // Transaction is rolled back when accessing version store. It was earlier marked as victim when the version store was shrunk due to insufficient space in tempdb. This transaction was marked as a victim earlier because it may need the row version(s) that have already been removed to make space in tempdb. Retry the transaction
         3960, // Snapshot isolation transaction aborted due to update conflict. You cannot use snapshot isolation to access table '%.*ls' directly or indirectly in database '%.*ls' to update, delete, or insert the row that has been modified or deleted by another transaction. Retry the transaction or change the isolation level for the update/delete statement.
         3957, // Snapshot isolation transaction failed in database '%.*ls' because the database did not allow snapshot isolation when the current transaction started. It may help to retry the transaction.
         3953, // Snapshot isolation transaction failed in database '%.*ls' because the database was not recovered when the current transaction was started. Retry the transaction after the database has recovered.
         3950, // Version store scan timed out when attempting to read the next row. Please try the statement again later when the system is not as busy.
         3948, // The transaction was terminated because of the availability replica config/state change or because ghost records are being deleted on the primary and the secondary availability replica that might be needed by queries running under snapshot isolation. Retry the transaction.
         3947, // The transaction was aborted because the secondary compute failed to catch up redo. Retry the transaction.
         3941, // The transaction cannot modify an object that is published for replication or has Change Data Capture enabled because the transaction started before replication or Change Data Capture was enabled on the database. Retry the transaction.
         3935, // A FILESTREAM transaction context could not be initialized. This might be caused by a resource shortage. Retry the operation.
         3635, // An error occurred while processing '%ls' metadata for database id %d, file id %d, and transaction='%.*ls'. Additional Context='%ls'. Location='%hs'(%d). Retry the operation; if the problem persists, contact the database administrator to review locking and memory configurations. Review the application for possible deadlock conflicts.
         3429, // Recovery could not determine the outcome of a cross-database transaction %S_XID, named '%.*ls', in database '%.*ls' (database ID %d:%d). The coordinating database (database ID %d:%d) was unavailable. The transaction was assumed to be committed. If the transaction was not committed, you can retry recovery when the coordinating database is available.
         2816, // The metadata for object with id %d has changed. Retry the statement.
         2021, // The referenced entity '%.*ls' was modified during DDL execution. Please retry the operation.
         1807, // Could not obtain exclusive lock on database 'model'. Retry the operation later.
         1535, // Cannot share extent %S_PGID. Shared extent directory is full. Retry the transaction. If the problem persists, contact Technical Support.
         1534, // Extent %S_PGID not found in shared extent directory. Retry the transaction. If the problem persists, contact Technical Support.
         1533, // Cannot share extent %S_PGID. The correct extents could not be identified. Retry the transaction.
         1532, // New sort run starting on page %S_PGID found an extent not marked as shared. Retry the transaction. If the problem persists, contact Technical Support.
         1438, // The server instance %ls rejected configure request; read its error log file for more information. The reason %u, and state %u, can be of use for diagnostics by Microsoft. This is a transient error hence retrying the request is likely to succeed. Correct the cause if any and retry.
         1421, // Communications to the remote server instance '%.*ls' failed to complete before its timeout. The ALTER DATABASE command may have not completed. Retry the command.
         1413, // Communications to the remote server instance '%.*ls' failed before database mirroring was fully started. The ALTER DATABASE command failed. Retry the command when the remote database is started.
         1404, // The command failed because the database mirror is busy. Reissue the command later.
         1232, // Failed to acquire lock with lock manager service, it could be due to many reasons including transient service failure.
         1222, // Lock request time out period exceeded.
         1221, // The Database Engine is attempting to release a group of locks that are not currently held by the transaction. Retry the transaction. If the problem persists, contact your support provider.
         1216, // The DDL statement with ABORT_AFTER_WAIT = BLOCKERS option cannot be completed due to a conflicting system task. The request can abort only user transactions. Please wait for the system task to complete and retry.
         1215, // A conflicting ABORT_AFTER_WAIT = BLOCKERS request is waiting for existing transactions to rollback. This request cannot be executed. Please retry when the previous request is completed.
         1205, // Transaction (Process ID %d) was deadlocked on %.*ls resources with another process and has been chosen as the deadlock victim. Rerun the transaction.
         1204, // The instance of the SQL Server Database Engine cannot obtain a LOCK resource at this time. Rerun your statement when there are fewer active users. Ask the database administrator to check the lock and memory configuration for this instance, or to check for long-running transactions.
         1203, // Process ID %d attempted to unlock a resource it does not own: %.*ls. Retry the transaction, because this error may be caused by a timing condition. If the problem persists, contact the database administrator.
          997, // A connection was successfully established with the server, but then an error occurred during the login process. (provider: Named Pipes Provider, error: 0 - Overlapped I/O operation is in progress)
          988, // Unable to access database '%.*ls' because it lacks a quorum of nodes for high availability. Try the operation again later.
          982, // Unable to access the '%.*ls' database because no online secondary replicas are enabled for read-only access. Check the availability group configuration to verify that at least one secondary replica is configured for read-only access. Wait for an enabled replica to come online, and retry your read-only operation.
          952, // Database '%.*ls' is in transition. Try the statement later.
          941, // Database '%.*ls' cannot be opened because it is not started. Retry when the database is started.
          927, // Database '%.*ls' cannot be opened. It is in the middle of a restore.
          926, // Database '%.*ls' cannot be opened. It has been marked SUSPECT by recovery. See the SQL Server errorlog for more information.
          921, // Database '%.*ls' has not been recovered yet. Wait and try again.
          669, // The row object is inconsistent. Please rerun the query.
          617, // Descriptor for object ID %ld in database ID %d not found in the hash table during attempt to un-hash it. A work table is missing an entry. Rerun the query. If a cursor is involved, close and reopen the cursor.
          615, // Could not find database ID %d, name '%.*ls'. The database may be offline. Wait a few minutes and try again.
          601, // Could not continue scan with NOLOCK due to data movement.
          539, // Schema changed after the target table was created. Rerun the Select Into query.
          233, // The client was unable to establish a connection because of an error during connection initialization process before login. Possible causes include the following: the client tried to connect to an unsupported version of SQL Server; the server was too busy to accept new connections; or there was a resource limitation (insufficient memory or maximum allowed connections) on the server. (provider: TCP Provider, error: 0 - An existing connection was forcibly closed by the remote host.)
          121, // The semaphore timeout period has expired
           64, // A connection was successfully established with the server, but then an error occurred during the login process. (provider: TCP Provider, error: 0 - The specified network name is no longer available.)

        // Omitted:
        
        // 203 // A connection was successfully established with the server, but then an error occurred during the pre-login handshake. (provider: TCP Provider, error: 0 - 20) ---> System.ComponentModel.Win32Exception (203): Unknown error: 203 
        // ^^^ EF Core considers this error to be transient only when the inner
        //     exception is a Win32Exception.  SqlRetryLogicBaseProvider sees
        //     only error codes and thus cannot distinguish this case.

        //  20 // The instance of SQL Server you attempted to connect to does not support encryption.
        //  ^^ In the author's opinion, this is not a transient error.
    };
}
