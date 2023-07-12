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

    // -ThrottleLimit
    // Maximum count of operations to perform in parallel.  The default value
    // is the number of virtual processors on the local machine.
    [Parameter()]
    [Alias("Parallelism")]
    [ValidateRange(1, int.MaxValue)]
    public int ThrottleLimit { get; set; }

    private readonly SqlContextParallelSet _set = new();

    protected override void ProcessRecord()
    {
        if (Context is not null)
            foreach (var context in Context)
                _set.Contexts.Add(context);
    }

    protected override void EndProcessing()
    {
        if (Name is not null)
            _set.Name = Name;

        if (ThrottleLimit > 0)
            _set.Parallelism = ThrottleLimit;

        WriteObject(_set);
    }
}
