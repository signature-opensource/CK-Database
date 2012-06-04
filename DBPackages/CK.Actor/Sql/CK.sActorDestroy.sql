-- Version = 1.0.0, Package = CK.Actor
--
-- Deletes an Actor.
--
create procedure CK.sActorDestroy
(
	@ActorId int,
	@ActorIdToDestroy int
)
as begin

	if @ActorIdToDestroy is not null 
	begin
		delete from CK.tActor where ActorId = @ActorIdToDestroy;
	end

	return 0;
end