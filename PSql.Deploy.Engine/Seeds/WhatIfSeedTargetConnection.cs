// Copyright Subatomix Research Inc.
// SPDX-License-Identifier: MIT

namespace PSql.Deploy.Seeds;

internal class WhatIfSeedTargetConnection : WhatIfTargetConnection, ISeedTargetConnection
{
    /// <summary>
    ///   Initializes a new <see cref="WhatIfSeedTargetConnection"/>
    ///   instance wrapping the specified connection.
    /// </summary>
    /// <inheritdoc cref="WhatIfTargetConnection(ITargetConnection)"/>
    public WhatIfSeedTargetConnection(ISeedTargetConnection connection)
        : base(connection) { }

#if NEED_UNDERLYING_CONNECTION
    /// <inheritdoc cref="WhatIfTargetConnection.UnderlyingConnection"/>
    protected new ISeedTargetConnection UnderlyingConnection
        => (ISeedTargetConnection) base.UnderlyingConnection;
#endif

    /// <inheritdoc/>
    public Task PrepareAsync(Guid runId, int workerId, CancellationToken cancellation = default)
    {
        Log("Would prepare connection.");
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task ExecuteSeedBatchAsync(string sql, CancellationToken cancellation = default)
    {
        ArgumentNullException.ThrowIfNull(sql);

        Log("Would execute batch beginning with: " + GetInitialContent(sql));
        return Task.CompletedTask;
    }

    // internal for testing
    internal static string GetInitialContent(string s)
    {
        int index;

        // Find start
        for (index = 0;; index++)
        {
            if (index >= s.Length)
                return "";

            var c = s[index];

            if (!char.IsWhiteSpace(c)) // 'space' includes \r and \n here
                break;
        }

        var start = index;
        var end   = index;

        // Find end
        for (index++;; index++)
        {
            if (index >= s.Length)
                break;

            var c = s[index];

            if (c is '\r' or '\n')
                break;

            if (!char.IsWhiteSpace(c))
                end = index;
        }

        return s[start..(end + 1)];
    }
}
