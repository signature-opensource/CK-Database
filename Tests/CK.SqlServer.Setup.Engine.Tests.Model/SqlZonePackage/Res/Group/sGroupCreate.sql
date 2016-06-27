-- SetupConfig: { "Requires" : [ "CK.sActorCreate" ] }
create procedure CK.sGroupCreate 
(
	@SecurityZoneId int = 0,
	@GroupName varchar( 32 ),
	@GroupIdResult int output
)
as
begin
	--[beginsp]
	select @GroupIdResult = GroupId from CK.tGroup where SecurityZoneId = @SecurityZoneId and GroupName = @GroupName;
	if @@rowcount = 0
	begin
		exec CK.sActorCreate @GroupIdResult output;
		insert into CK.tGroup( GroupId, SecurityZoneId, GroupName ) values ( @GroupIdResult, @SecurityZoneId, @GroupName );
	end
	--[endsp]
end

