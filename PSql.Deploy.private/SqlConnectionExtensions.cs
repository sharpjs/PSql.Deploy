// Copyright 2023 Subatomix Research Inc.
// SPDX-License-Identifier: ISC

namespace PSql.Deploy;

internal static class SqlConnectionExtensions
{
    public static SqlConnection GetRealSqlConnection(this ISqlConnection connection)
        => (SqlConnection) connection.UnderlyingConnection;

    public static SqlCommand GetRealSqlCommand(this ISqlCommand command)
        => (SqlCommand) command.UnderlyingCommand;
}
