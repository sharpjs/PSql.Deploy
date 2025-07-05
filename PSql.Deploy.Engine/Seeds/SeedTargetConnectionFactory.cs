// Copyright Subatomix Research Inc.
// SPDX-License-Identifier: MIT

namespace PSql.Deploy.Seeds;

internal delegate ISeedTargetConnection SeedTargetConnectionFactory(
    Target            target,
    ISqlMessageLogger logger
);
