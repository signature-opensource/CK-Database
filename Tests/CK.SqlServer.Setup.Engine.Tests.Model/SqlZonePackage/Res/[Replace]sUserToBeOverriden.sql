-- Version = 1.0.0
-- This is from the SqlZonePackage : it is overridden by UserTable with a new method: there is a new parameter to it!
create procedure CK.sUserToBeOverriden
(
	@Param1 int,
	@ParamFromZone int = 0, -- New partameters MUST have a default value that makes sense!!
	@Done bit output
)
as
begin
	--[beginsp]
	
	set @Done = 1;

	--[endsp]
end

