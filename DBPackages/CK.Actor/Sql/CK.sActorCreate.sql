-- Version = 1.0.0, Package = CK.Actor
--
-- Finds or creates an Actor.
--
create procedure CK.sActorCreate 
(
	@ActorId int,
	@ActorIdResult int output
)
as begin

	insert into CK.tActor DEFAULT VALUES;
	set @ActorIdResult = @@IDENTITY;
	
	return 0;
end