-- Version = 1.0.0, Package = CK.ActorGroup
--
-- Add an actor (typically a user) to a security zone.
--
alter procedure CK.sSecurityZoneActorAdd
(
	@ActorId int,
	@UserActorId int,
	@SecurityZoneId int
)
as begin
	--[beginsp]
	if exists (select * from CK.tGroup where GroupId = @SecurityZoneId )
	begin
		exec CK.sGroupActorAdd @ActorId, @UserActorId, @SecurityZoneId;
		return 0;
	end

	return 1;
	--[endsp]
end