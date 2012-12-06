-- Version = 2.12.3, Requires = { CK.sActorCreate }
create procedure CK.sGroupCreate 
(
	@GroupName varchar( 32 ),
	@GroupIdResult int output
)
as
begin
	--[beginsp]
	select @GroupIdResult = GroupId from CK.tGroup where GroupName = @GroupName;
	if @@rowcount = 0
	begin
		exec CK.sActorCreate @GroupIdResult output;
		insert into CK.tGroup( GroupId, GroupName ) values ( @GroupIdResult, @GroupName );
	end
	--[endsp]
end

