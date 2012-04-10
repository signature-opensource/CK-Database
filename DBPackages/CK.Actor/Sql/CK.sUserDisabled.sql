-- Version = 1.0.0, Package = CK.Actor
--
-- Disabled an user
--
create procedure CK.sUserDisabled
	@UserId int
as begin

	if @UserId is not null 
	begin 

		select UserName from CK.tUser where UserId = @UserId;

		if @@RowCount = 1 update CK.tUser set IsEnabled = 0 where UserId = @UserId;

	end
	
	return 0;
end