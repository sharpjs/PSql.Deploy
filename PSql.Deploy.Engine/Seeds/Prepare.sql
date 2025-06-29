-- Copyright Subatomix Research Inc.
-- SPDX-License-Identifier: MIT

SET CONTEXT_INFO @RunId;

EXEC sp_set_session_context N'RunId',    @RunId,    @read_only = 1;
EXEC sp_set_session_context N'WorkerId', @WorkerId, @read_only = 1;
