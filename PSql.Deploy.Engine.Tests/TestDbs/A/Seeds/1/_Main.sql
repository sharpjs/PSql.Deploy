PRINT 'This is in the initial module.';
--# MODULE: a
--# PROVIDES: x
PRINT 'This is in module a.';
--# MODULE: b
--# REQUIRES: x
PRINT 'This is in module b.';

