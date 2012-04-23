-- Version = 1.0.1, Package = CK.Actor
--
-- Deletes an User. Deletes the actor facet is optional.
--
create procedure CK.sUserDestroy
(
	@ActorId int,
	@UserId int,
	@DestroyActor bit
)
as begin

	if @UserId is null 
	begin
		return 0;
	end

	delete from CK.tUser where UserId = @UserId;

	if @DestroyActor = 1 
	begin
		delete from CK.tActor where ActorId = @UserId;
	end

	return 0;
end