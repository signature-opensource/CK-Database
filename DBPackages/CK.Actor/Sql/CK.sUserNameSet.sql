-- Version = 1.0.0, Package = CK.Actor
--
-- Set a new UserName value for an user
--
create procedure CK.sUserNameSet 
	@UserId int,
	@UserName varchar ( 100 )
as begin

	if @UserId is not null 
	begin 

		select UserName from CK.tUser where UserId = @UserId;

		if @@RowCount = 1 update CK.tUser set UserName = @UserName where UserId = @UserId;

	end
	
	return 0;
end