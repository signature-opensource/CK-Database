-- Version = 1.0.1, Package = CK.Actor
--
-- Set a new Email value for an user
--
create procedure CK.sUserEmailSet 
(
	@ActorId int,
	@UserId int,
	@Email nvarchar ( 100 )
)
as begin

	if @UserId is not null 
	begin 
		update CK.tUser set Email = @Email where UserId = @UserId;
	end
	
	return 0;
end