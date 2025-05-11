// Copyright Subatomix Research Inc.
// SPDX-License-Identifier: MIT

namespace PSql.Deploy;

using static CommandBehavior;

/// <inheritdoc cref="ITargetConnection"/>
internal class SqlTargetConnection : ITargetConnection
{
    private readonly SqlConnection _connection;
    private readonly SqlCommand    _command;

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
    public SqlTargetConnection(Target target, ISqlMessageLogger logger)
    {
        if (target is null)
            throw new ArgumentNullException(nameof(target));
        if (logger is null)
            throw new ArgumentNullException(nameof(logger));

        Target = target;
        Logger = logger;

        _connection = target.Credential is { } credential
            ? new SqlConnection(target.ConnectionString, credential)
            : new SqlConnection(target.ConnectionString);

        _connection.RetryLogicProvider                = RetryLogicProvider;
        _connection.FireInfoMessageEventOnUserErrors  = true;
        _connection.InfoMessage                      += HandleMessage;
        _connection.Disposed                         += HandleUnexpectedDisposal;

        _command                    = _connection.CreateCommand();
        _command.CommandType        = CommandType.Text;
        _command.CommandTimeout     = 0; // No timeout
        _command.RetryLogicProvider = RetryLogicProvider;
    }

    public static SqlRetryLogicBaseProvider RetryLogicProvider { get; }
        = SqlConfigurableRetryFactory.CreateExponentialRetryProvider(new()
        {
            NumberOfTries   = 5,
            DeltaTime       = TimeSpan.FromSeconds(2),
            MaxTimeInterval = TimeSpan.FromMinutes(2),
        });

    /// <summary>
    ///   Gets an object representing the target database.
    /// </summary>
    public Target Target { get; }

    /// <summary>
    ///   Gets the logger for server messages received over the connection.
    /// </summary>
    public ISqlMessageLogger Logger { get; }

    /// <summary>
    ///   Gets whether one or more error messages have been received over the
    ///   connection since the most recent invocation of
    ///   <see cref="ClearErrors"/>.
    /// </summary>
    public bool HasErrors { get; private set; }

    /// <summary>
    ///   Throws an exception if one or more error messages have been received
    ///   over the connection since the most recent invocation of
    ///   <see cref="ClearErrors"/>.
    /// </summary>
    /// <exception cref="DataException">
    ///   One or more error messages have been received over the connection
    ///   since the most recent invocation of <see cref="ClearErrors"/>.
    /// </exception>
    public void ThrowIfHasErrors()
    {
        if (HasErrors)
            throw new DataException("An error occurred while executing the SQL batch.");
    }

    /// <summary>
    ///   Resets error-handling state, forgetting any error messages that have
    ///   been received over the connection.
    /// </summary>
    public void ClearErrors()
    {
        HasErrors = false;
    }

    /// <inheritdoc/>
    public Task OpenAsync(CancellationToken cancellation)
    {
        return _connection.OpenAsync(cancellation);
    }

    /// <inheritdoc/>
    public async Task ExecuteAsync(
        string            sql,
        CancellationToken cancellation)
    {
        ClearErrors();

        _command.CommandText = sql;

        await _command.ExecuteNonQueryAsync(cancellation);

        ThrowIfHasErrors();
    }

    /// <inheritdoc/>
    public async Task ExecuteAsync<T>(
        string                 sql,
        Action<IDataRecord, T> consumer,
        T                      state,
        CancellationToken      cancellation)
    {
        ClearErrors();

        _command.CommandText = sql;

        await using var reader = await _command.ExecuteReaderAsync(
            SequentialAccess | SingleResult, cancellation
        );

        while (await reader.ReadAsync(cancellation))
            consumer(reader, state);

        ThrowIfHasErrors();
    }

    /// <inheritdoc/>
    public virtual void Dispose()
    {
        // Disposal is now expected
        _connection.Disposed -= HandleUnexpectedDisposal;

        _command   .Dispose();
        _connection.Dispose();
    }

    /// <inheritdoc/>
    public virtual async ValueTask DisposeAsync()
    {
        // Disposal is now expected
        _connection.Disposed -= HandleUnexpectedDisposal;

        await _command   .DisposeAsync();
        await _connection.DisposeAsync();
    }

    private void HandleMessage(object sender, SqlInfoMessageEventArgs e)
    {
        const int    MaxInformationalSeverity = 10;
        const string NonProcedureLocationName = "(batch)";

        foreach (SqlError? error in e.Errors)
        {
            if (error is null)
                continue;

            // Mark current command if failed
            HasErrors |= error.Class <= MaxInformationalSeverity;

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
