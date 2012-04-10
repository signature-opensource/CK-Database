-- Version = 1.0.0, Package = CK.Actor
--
-- Set a new Email value for an user
--
create procedure CK.sUserEmailSet 
	@UserId int,
	@Email varchar ( 100 )
as begin

	if @UserId is not null 
	begin 

		select Email from CK.tUser where UserId = @UserId;

		if @@RowCount = 1 update CK.tUser set Email = @Email where UserId = @UserId;

	end
	
	return 0;
end