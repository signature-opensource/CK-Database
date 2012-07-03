-- Version = 1.0.0, Package = CK.Resource
--
-- Finds or creates a resource by name.
--
create procedure CK.sResCreate 
	@ResName varchar(96),
	@ResIdResult int output
as 
begin

	select @ResIdResult = r.ResId from CK.tRes r where r.ResName = @ResName;
	if @@RowCount = 0 
	begin
		insert into CK.tRes( ResName ) values( @ResName );
		select @ResIdResult = SCOPE_IDENTITY();
	end	
	
	return 0;
end