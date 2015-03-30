-- Version = 1.0.0
-- This is from the SqlZonePackage : it is overridden by UserTable the Zone Package object with a [SqlObjectItem] attribute.
-- This is typically done when there is no new parameter to the stored procedure so that we do not need a new method signature to call it.
create procedure CK.sUserToBeOverridenIndirect
(
	@Param1 int,
	@Done bit output
)
as
begin
	--[beginsp]
	
	set @Done = 1;

	--[endsp]
end

