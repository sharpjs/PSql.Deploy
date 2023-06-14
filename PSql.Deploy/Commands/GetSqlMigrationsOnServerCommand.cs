// Copyright 2023 Subatomix Research Inc.
// SPDX-License-Identifier: ISC

using PSql.Deploy.Migrations;

namespace PSql.Deploy;

[Cmdlet(VerbsCommon.Get, "SqlMigrationsOnServer")]
[OutputType(typeof(Migration))]
public class GetSqlMigrationsOnServerCommand : Cmdlet
{
}
