-- SetupConfig: { 
--		"Requires": ["CK.sUserToBeOverriden"] 
--	}
create procedure CK.sUserExists2
(
	@UserPart1 int,
	@UserPart2 int,
	@ExistsResult bit output
)
as
begin
	--[beginsp]
	if exists( select * from CK.tUser where UserName = cast(@UserPart1 as varchar) + cast(@UserPart2 as varchar) )
		set @ExistsResult = 1;
	else set @ExistsResult = 0;
	--[endsp]
end

