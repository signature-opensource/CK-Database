-- Version = 1.0.0, Package = CK.ActorGroup
--
-- Add an actor (typically a user) to a security zone.
--
create procedure CK.sSecurityZoneActorAdd
(
	@ActorId int,
	@UserActorId int,
	@SecurityZoneId int
)
as begin

	if exists (select * from dbo.tGroup where GroupId = @SecurityZoneId )
	begin
		exec CK.sGroupActorAdd @ActorId, @UserActorId, @SecurityZoneId;
		return 0;
	end

	return 1;
end