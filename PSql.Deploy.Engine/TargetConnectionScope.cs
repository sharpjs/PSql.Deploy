// Copyright Subatomix Research Inc.
// SPDX-License-Identifier: MIT

namespace PSql.Deploy;

internal class TargetConnectionScope : IDisposable, IAsyncDisposable
{
    internal static DbProviderFactory DbProviderFactory { get; set; }
        = SqlClientFactory.Instance;

    public static SqlRetryLogicBaseProvider RetryLogicProvider { get; }
        = SqlConfigurableRetryFactory.CreateExponentialRetryProvider(new()
        {
            NumberOfTries   = 5,
            DeltaTime       = TimeSpan.FromSeconds(2),
            MaxTimeInterval = TimeSpan.FromMinutes(2),
        });

    public TargetConnectionScope(Target target, ISqlMessageLogger logger)
    {
        if (target is null)
            throw new ArgumentNullException(nameof(target));
        if (logger is null)
            throw new ArgumentNullException(nameof(logger));

        Target = target;
        Logger = logger;

        var con = (SqlConnection) DbProviderFactory.CreateConnection()!;

        con.Credential = target.Credential;

        Connection = target.Credential is { } credential
            ? new SqlConnection(target.ConnectionString, credential)
            : new SqlConnection(target.ConnectionString);

        Connection.RetryLogicProvider                = RetryLogicProvider;
        Connection.FireInfoMessageEventOnUserErrors  = true;
        Connection.InfoMessage                      += HandleMessage;
        Connection.Disposed                         += HandleUnexpectedDisposal;
    }

    public Target Target { get; }

    public ISqlMessageLogger Logger { get; }

    public SqlConnection Connection { get; }

    public bool HasErrors { get; private set; }

    public void ThrowIfHasErrors()
    {
        if (HasErrors)
            throw new DataException("An error occurred while executing the SQL batch.");
    }

    public void ClearErrors()
    {
        HasErrors = false;
    }

    public virtual void Dispose()
    {
        // Disposal is now expected
        Connection.Disposed -= HandleUnexpectedDisposal;
        Connection.Dispose();
    }

    public virtual ValueTask DisposeAsync()
    {
        // Disposal is now expected
        Connection.Disposed -= HandleUnexpectedDisposal;
        return Connection.DisposeAsync();
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

    //private static string Format(SqlError error)
    //{
    //    const string NonProcedureLocationName = "(batch)";

    //    var procedure
    //        =  error.Procedure.NullIfEmpty()
    //        ?? NonProcedureLocationName;

    //    return $"{procedure}:{error.LineNumber}: E{error.Class}: {error.Message}";
    //}

    [DoesNotReturn]
    private static void HandleUnexpectedDisposal(object? sender, EventArgs e)
    {
        throw new DataException(
            "The connection to the database server was closed unexpectedly."
        );
    }
}
