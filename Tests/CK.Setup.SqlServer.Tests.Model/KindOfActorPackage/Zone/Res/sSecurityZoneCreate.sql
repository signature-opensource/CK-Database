-- Version = 2.12.3, Requires = { CK.sZoneGroupCreate }
create procedure CK.sUserCreate 
(
	@ZoneName varchar( 12 ),
	@SecurityZoneIdResult int output
)
as
begin
	--[beginsp]
	select @SecurityZoneIdResult = SecurityZoneIdId from CK.tSecurityZone where ZoneName = @ZoneName;
	if @@rowcount = 0
	begin
		exec CK.sZoneGroupCreate @SecurityZoneIdResult output;
		insert into CK.tUser( UserId, UserName, Email ) values ( @UserIdResult, @UserName, '' );
	end
	--[endsp]
end

