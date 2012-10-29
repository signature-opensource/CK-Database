-- Version = 1.0.0, Package = CK.ActorGroup
--
-- Add an actor (typically a user) to a group.
--
alter procedure CK.sGroupActorAdd 
(
	@ActorId int,
	@UserActorId int,
	@GroupId int
)
as begin
	--[beginsp]
	if not exists (select * from CK.tActorProfile where GroupId = @GroupId and ActorId = @UserActorId)
	begin
		insert into CK.tActorProfile(ActorId, GroupId) 
			values(@UserActorId, @GroupId);
		return 0;
	end
	return 1;
	--[endsp]
end