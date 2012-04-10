-- Version = 1.0.0, Package = CK.Actor
--
-- Deletes an User.
--
create procedure CK.sUserDestroy
	@UserId int,
	@DestroyActor bit
as begin

	if @UserId is null return 0;
	else DELETE FROM CK.tUser WHERE UserId = @UserId;

	if @DestroyActor = 1 DELETE FROM CK.tActor WHERE ActorId = @UserId;
	
	return 0;
end