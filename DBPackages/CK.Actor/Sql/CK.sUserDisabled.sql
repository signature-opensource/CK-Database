-- Version = 1.0.1, Package = CK.Actor
--
-- Disabled an user
--
create procedure CK.sUserDisabled
(
	@ActorId int,
	@UserId int
)
as begin

	if @UserId is not null 
	begin 
		update CK.tUser set IsEnabled = 0 where UserId = @UserId;
	end
	
	return 0;
end