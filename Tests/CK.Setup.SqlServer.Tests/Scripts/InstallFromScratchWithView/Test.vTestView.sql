-- Version = 1.0.0, Package = Test.WithView
-- Requires = { Test.tTest }
create view Test.vTestView
as
	select Id from Test.tTest;