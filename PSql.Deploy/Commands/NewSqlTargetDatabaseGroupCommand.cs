// Copyright Subatomix Research Inc.
// SPDX-License-Identifier: MIT

namespace PSql.Deploy.Commands;

/// <summary>
///   The <c>New-SqlTargetSet</c> cmdlet.
/// </summary>
[Cmdlet(VerbsCommon.New, "SqlTargetDatabaseGroup")]
[OutputType(typeof(SqlTargetDatabaseGroup))]
public class NewSqlTargetDatabaseGroupCommand : PSCmdlet
{
    /// <summary>
    ///   <b>-Target:</b>
    ///   Objects specifying the databases in the target set.
    /// </summary>
    [Parameter(Position = 0, Mandatory = true, ValueFromPipeline = true)]
    [ValidateNotNullOrEmpty]
    public SqlTargetDatabase[]? Target { get; set; }

    /// <summary>
    ///   <b>-Name:</b>
    ///   Informational name of the target set.
    /// </summary>
    [Parameter(Position = 1)]
    [ValidateNotNullOrEmpty]
    public string? Name { get; set; }

    /// <summary>
    ///   <b>-MaxParallelism:</b>
    ///   Maximum count of operations to perform in parallel.  The default
    ///   value is the number of logical processors on the local machine.
    /// </summary>
    [Parameter()]
    [ValidateRange(1, int.MaxValue)]
    public int MaxParallelism { get; set; }

    /// <summary>
    ///   <b>-MaxParallelismPerDatabase:</b>
    ///   Maximum count of operations to perform in parallel against any one
    ///   database. The default value is the number of logical processors on
    ///   the local machine.
    /// </summary>
    [Parameter()]
    [ValidateRange(1, int.MaxValue)]
    public int MaxParallelismPerDatabase { get; set; }

    // Collected targets from all ProcessRecord invocations
    private IReadOnlyList<SqlTargetDatabase>? _targets;

    /// <inheritdoc/>
    protected override void ProcessRecord()
    {
        // Assume that validation has occurred
        Assume.NotNull(Target);

        if (_targets is null)
            _targets = Target;
        else
            PromoteToList(ref _targets).AddRange(Target);
    }

    /// <inheritdoc/>
    protected override void EndProcessing()
    {
        // Assume that ProcessRecord has been invoked
        Assume.NotNull(_targets);

        WriteObject(new SqlTargetDatabaseGroup(
            _targets, Name, MaxParallelism, MaxParallelismPerDatabase
        ));
    }

    private static List<T> PromoteToList<T>(ref IReadOnlyList<T> collection)
    {
        if (collection is List<T> list)
            return list;

        collection = list = collection.ToList();
        return list;
    }
}
