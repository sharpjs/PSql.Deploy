// Copyright Subatomix Research Inc.
// SPDX-License-Identifier: MIT

using PSql.Deploy.Utilities;

namespace PSql.Deploy.Commands;

/// <summary>
///   The <c>New-SqlTargetSet</c> cmdlet.
/// </summary>
[Cmdlet(VerbsCommon.New, "SqlTargetSet")]
[OutputType(typeof(TargetSet))]
public class NewSqlTargetSetCommand : PSCmdlet
{
    /// <summary>
    ///   <b>-Target:</b>
    ///   Objects specifying how to connect to the databases in the target set.
    /// </summary>
    [Parameter(Position = 0, Mandatory = true, ValueFromPipeline = true)]
    [ValidateNotNullOrEmpty]
    public object[]? Target { get; set; }

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
    ///   Maximum count of operations to perform in parallel against one
    ///   database. The default value is the number of logical processors on
    ///   the local machine.
    /// </summary>
    [Parameter()]
    [ValidateRange(1, int.MaxValue)]
    public int MaxParallelismPerDatabase { get; set; }

    // Collected targets from all ProcessRecord invocations
    private IReadOnlyList<Target>? _targets;

    /// <inheritdoc/>
    protected override void ProcessRecord()
    {
        //var targets = Target.Sanitize();
        //if (targets.Length is 0)
        //    return;

        //var realTargets = Array.ConvertAll(targets, Coerce.ToTarget);

        //if (_targets is null)
        //    _targets = realTargets!;
        //else
        //    PromoteToList(ref _targets).AddRange(realTargets!);
    }

    /// <inheritdoc/>
    protected override void EndProcessing()
    {
        var set = new TargetSet(
            _targets ?? [],
            Name.NullIfEmpty(),
            MaxParallelism,
            MaxParallelismPerDatabase
        );

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
