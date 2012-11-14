-- Version = 1.0.0, Package = CK.ActorGroup
--
-- Creates a Group.
--
create procedure CK.sGroupCreate 
(
	@ActorId int,
	@SecurityZoneId int,
	@GroupName nvarchar(32),
	@GroupIdResult int output
)
as begin
	--[beginsp]
	declare @GroupId int;
	exec CK.sActorCreate @ActorId, @GroupId output;

	insert into CK.tGroup(GroupId, SecurityZoneId, GroupName) 
		values(@GroupId, @SecurityZoneId, @GroupName);
	set @GroupIdResult = @GroupId;
		
	return 0;
	--[endsp]
end