-- Version = 1.0.0, Package = CK.ActorGroup
--
-- Remove an actor (typically a user) from a group.
--
create procedure CK.sGroupActorRemove
(
	@ActorId int,
	@UserActorId int,
	@GroupId int
)
as begin
	
	if exists (select * from dbo.tActorProfile where GroupId = @GroupId and ActorId = @UserActorId)
	begin
		delete from CK.tActorProfile where GroupId = @GroupId and ActorId = @UserActorId;
		return 0;
	end
	
	return 1;

end