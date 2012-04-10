-- Version = 1.0.0, Requires={ Test.sOneStoredProcedure }
-- This one says nothing about its package: it can be added to the content of any package (see Test.WithModel.ck).
create function Test.fTest( @i int ) returns int
as
begin
	return @i*@i;
end
