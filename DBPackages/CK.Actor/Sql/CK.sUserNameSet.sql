-- Version = 1.0.1, Package = CK.Actor
--
-- Set a new UserName value for an user
--
create procedure CK.sUserNameSet 
(
	@ActorId int,
	@UserId int,
	@UserName varchar ( 100 )
)
as begin

	if @UserId is not null 
	begin 
		update CK.tUser set UserName = @UserName  where UserId = @UserId;
	end
	
	return 0;
end