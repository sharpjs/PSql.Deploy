// Copyright 2023 Subatomix Research Inc.
// SPDX-License-Identifier: ISC

namespace PSql.Deploy.Commands;

[Cmdlet(VerbsCommon.New, "SqlContextParallelSet")]
[OutputType(typeof(SqlContextParallelSet))]
public class NewSqlContextParallelSetCommand : PSCmdlet
{
    // -Name
    // Informational name of the set.
    [Parameter(Position = 0)]
    [ValidateNotNullOrEmpty]
    public string? Name { get; set; }

    // -Context
    // Objects specifying how to connect to the databases in the set.  Obtain
    // via the New-SqlContext cmdlet.
    [Parameter(Position = 1, ValueFromPipeline = true)]
    [ValidateNotNullOrEmpty]
    public SqlContext[]? Context { get; set; }

    // -Parallelism
    // Maximum count of operations to perform in parallel.  The default value
    // is the number of virtual processors on the local machine.
    [Parameter()]
    [Alias("ThrottleLimit")]
    [ValidateRange(1, int.MaxValue)]
    public int Parallelism { get; set; }

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

        if (Parallelism > 0)
            set.Parallelism = Parallelism;

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
