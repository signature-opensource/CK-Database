-- Version = 1.0.0, Package = CK.ActorGroup
--
-- Remove an actor (typically a user) from a security zone.
--
alter procedure CK.sSecurityZoneActorRemove
(
	@ActorId int,
	@UserActorId int,
	@SecurityZoneId int
)
as begin

	if exists (select * from CK.tGroup where GroupId = @SecurityZoneId )
	begin
		exec CK.sGroupActorRemove @ActorId, @UserActorId, @SecurityZoneId;
		return 0;
	end

	return 1;
end