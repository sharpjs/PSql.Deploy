// Copyright Subatomix Research Inc.
// SPDX-License-Identifier: MIT

namespace PSql.Deploy.Migrations;

internal delegate IMigrationTargetConnection MigrationTargetConnectionFactory(
    Target            target,
    ISqlMessageLogger logger
);
