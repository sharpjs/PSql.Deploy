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
            ? new SqlConnection(target.ConnectionString, credential)
            : new SqlConnection(target.ConnectionString);

        Connection.RetryLogicProvider                = RetryLogicProvider;
        Connection.FireInfoMessageEventOnUserErrors  = true;
        Connection.InfoMessage                      += HandleMessage;
        Connection.Disposed                         += HandleUnexpectedDisposal;

        Command                    = Connection.CreateCommand();
        Command.CommandType        = CommandType.Text;
        Command.RetryLogicProvider = RetryLogicProvider;
    }

    /// <summary>
    ///   Gets the retry logic for the connection.
    /// </summary>
    protected static SqlRetryLogicBaseProvider RetryLogicProvider { get; }
        = SqlConfigurableRetryFactory.CreateExponentialRetryProvider(new()
        {
            NumberOfTries   = 5,
            DeltaTime       = TimeSpan.FromSeconds(2),
            MaxTimeInterval = TimeSpan.FromMinutes(2),
        });

    /// <inheritdoc/>
    public Target Target { get; }

    /// <inheritdoc/>
    public ISqlMessageLogger Logger { get; }

    /// <summary>
    ///   Gets the underlying SqlClient connection.
    /// </summary>
    protected SqlConnection Connection { get; }

    /// <summary>
    ///   Gets the underlying SqlClient command.
    /// </summary>
    protected SqlCommand Command { get; }

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
}
