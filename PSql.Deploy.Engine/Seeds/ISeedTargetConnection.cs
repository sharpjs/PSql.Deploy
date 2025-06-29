// Copyright Subatomix Research Inc.
// SPDX-License-Identifier: MIT

namespace PSql.Deploy.Seeds;

/// <summary>
///   A connection to a target database for data seeding operations.
/// </summary>
internal interface ISeedTargetConnection : ITargetConnection
{
    /// <summary>
    ///   Prepares the connection for data seed operations asynchronously.
    /// </summary>
    /// <param name="runId">
    ///   A unique identifier for the seed application.
    /// </param>
    /// <param name="workerId">
    ///   The ordinal integer of the seed application worker thread.
    /// </param>
    /// <param name="cancellation">
    ///   A token to monitor for cancellation requests.
    /// </param>
    /// <returns>
    ///   A <see cref="Task"/> representing the asynchronous operation.
    /// </returns>
    /// <remarks>
    ///   <para>
    ///     This method populates the following contextual information in the
    ///     remote SQL session:
    ///   </para>
    ///   <list type="bullet">
    ///     <item>
    ///       <term>Run ID</term>
    ///       <description>
    ///         a random <c>uniqueidentifier</c> unique to the seed
    ///         application.  Available via:
    ///         <list type="bullet">
    ///           <item><c>SESSION_CONTEXT('RunId')</c></item>
    ///           <item><c>SESSION_CONTEXT('PSql.Deploy.RunId')</c></item>
    ///           <item><c>CONTEXT_INFO()</c></item>
    ///         </list>
    ///       </description>
    ///     </item>
    ///     <item>
    ///       <term>Worker ID</term>
    ///       <description>
    ///         an ordinal <c>int</c> specific to the seed application worker
    ///         thread.  Available via:
    ///         <list type="bullet">
    ///          <item><c>SESSION_CONTEXT('WorkerId')</c></item>
    ///          <item><c>SESSION_CONTEXT('PSql.Deploy.WorkerId')</c></item>
    ///         </list>
    ///       </description>
    ///     </item>
    ///   </list>
    /// </remarks>
    Task PrepareAsync(
        Guid              runId,
        int               workerId,
        CancellationToken cancellation = default
    );

    /// <summary>
    ///   Executes the specified seed content batch against the target
    ///   database asynchronously.
    /// </summary>
    /// <param name="sql">
    ///   The seed content batch to execute.
    /// </param>
    /// <param name="cancellation">
    ///   A token to monitor for cancellation requests.
    /// </param>
    /// <returns>
    ///   A <see cref="Task"/> representing the asynchronous operation.
    /// </returns>
    /// <remarks>
    ///   This method requires prior preparation of the connection via
    ///   <see cref="PrepareAsync"/>.
    /// </remarks>
    /// <exception cref="ArgumentNullException">
    ///   <paramref name="sql"/> is <see langword="null"/>.
    /// </exception>
    Task ExecuteSeedBatchAsync(
        string            sql,
        CancellationToken cancellation = default
    );
}
