-- Version = 1.0.0, Package = CK.Resource, Requires={ CK.sResRemove }
--
-- Deletes a BigText Data ressource
-- Deletes ressource too if @AllRes bit equals 1
--
create procedure CK.sResBigTextDataRemove
	@ResName	varchar(96),
	@AllRes		bit
as
begin

	declare @idRes int;
	
	select @idRes = ResId
		from CK.tRes
		where ResName = @ResName;
	
	if @@RowCount = 0 return 0
	
	delete from CK.tResBigTextData where ResId = @idRes;
	if @AllRes = 1 exec CK.sResRemove @ResName, @idRes;

	return 0;

end