// Copyright Subatomix Research Inc.
// SPDX-License-Identifier: MIT

namespace PSql.Deploy;

internal delegate ITargetConnection TargetConnectionFactory(
    Target            target,
    ISqlMessageLogger logger
);
