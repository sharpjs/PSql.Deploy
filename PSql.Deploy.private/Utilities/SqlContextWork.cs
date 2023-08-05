// Copyright 2023 Subatomix Research Inc.
// SPDX-License-Identifier: ISC

namespace PSql.Deploy.Utilities;

internal class SqlContextWork
{
    public SqlContextWork(SqlContext context)
    {
        if (context is null)
            throw new ArgumentNullException(nameof(context));

        Context = context;

        ServerDisplayName
            =  context.AsAzure?.ServerResourceName
            ?? context.ServerName
            ?? "local";

        DatabaseDisplayName
            =  context.DatabaseName
            ?? "default";

        FullDisplayName = string.Concat(
            ServerDisplayName, ".",
            DatabaseDisplayName
        );
    }

    public SqlContext Context { get; }

    public string ServerDisplayName { get; }

    public string DatabaseDisplayName { get; }

    public string FullDisplayName { get; }
}
