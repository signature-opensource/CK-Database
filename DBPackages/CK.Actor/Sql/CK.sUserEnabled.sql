-- Version = 1.0.1, Package = CK.Actor
--
-- Enabled an user
--
create procedure CK.sUserEnabled 
(
	@ActorId int,
	@UserId int
)
as begin

	if @UserId is not null 
	begin 
		update CK.tUser set IsEnabled = 1 where UserId = @UserId;
	end
	
	return 0;
end