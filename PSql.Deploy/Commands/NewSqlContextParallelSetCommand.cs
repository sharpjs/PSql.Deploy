// Copyright 2024 Subatomix Research Inc.
// SPDX-License-Identifier: ISC

namespace PSql.Deploy.Commands;

/// <summary>
///   The <c>New-SqlContextParallelSet</c> cmdlet.
/// </summary>
[Cmdlet(VerbsCommon.New, "SqlContextParallelSet")]
[OutputType(typeof(SqlContextParallelSet))]
public class NewSqlContextParallelSetCommand : PSCmdlet
{
    /// <summary>
    ///   <b>-Context:</b>
    ///   Objects specifying how to connect to the databases in the set.
    ///   Obtain via the <c>New-SqlContext</c> cmdlet.
    /// </summary>
    [Parameter(Position = 0, Mandatory = true, ValueFromPipeline = true)]
    [ValidateNotNullOrEmpty]
    public SqlContext[]? Context { get; set; }

    /// <summary>
    ///   <b>-Name:</b>
    ///   Informational name of the set.
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
    ///   Maximum count of operations to perform in parallel against one
    ///   database. The default value is the number of logical processors on
    ///   the local machine.
    /// </summary>
    [Parameter()]
    [ValidateRange(1, int.MaxValue)]
    public int MaxParallelismPerDatabase { get; set; }

    // Collected contexts from all ProcessRecord invocations
    private IReadOnlyList<SqlContext>? _contexts;

    protected override void ProcessRecord()
    {
        var contexts = Context.Sanitize();
        if (contexts.Length == 0)
            return;

        if (_contexts is null)
            _contexts = contexts;
        else
            PromoteToList(ref _contexts).AddRange(contexts);
    }

    protected override void EndProcessing()
    {
        var set = new SqlContextParallelSet();

        if (Name is { Length: > 0 })
            set.Name = Name;

        if (_contexts is not null)
            set.Contexts = _contexts;

        if (MaxParallelism > 0)
            set.MaxParallelism = MaxParallelism;

        if (MaxParallelismPerDatabase > 0)
            set.MaxParallelismPerDatabase = MaxParallelismPerDatabase;

        WriteObject(set);
    }

    private static List<T> PromoteToList<T>(ref IReadOnlyList<T> collection)
    {
        if (collection is List<T> list)
            return list;

        collection = list = collection.ToList();
        return list;
    }
}
