-- Version = 1.0.0, Package = CK.Resource
--
-- Removes a ressource
-- return the ResId of the deleted ressource
--
create procedure CK.sResRemove
	@ResName varChar(96),
	@ResIdResult int output
as begin
		
	select @ResIdResult = ResId from CK.tRes with (nolock) where ResName = @ResName;
		
	if @@RowCount > 0
	begin
		delete from CK.tRes where ResId = @ResIdResult;
	end

	return 0;
	
end