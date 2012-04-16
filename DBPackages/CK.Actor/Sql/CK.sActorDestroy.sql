-- Version = 1.0.0, Package = CK.Actor
--
-- Deletes an Actor.
--
create procedure CK.sActorDestroy
(
	@ActorId int
)
as begin

	if @ActorId is not null DELETE FROM CK.tActor WHERE ActorId = @ActorId;
	
	return 0;
end