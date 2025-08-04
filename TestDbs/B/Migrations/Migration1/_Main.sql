--# PRE
:r $(Path)\0-Pre.sql

--# CORE
-- This chunk is not present in TestDbs\A.
:r $(Path)\1-Core.sql

--# POST
:r $(Path)\2-Post.sql
