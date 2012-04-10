-- Version = 1.0.0, Package = CK.Actor
--
-- Enabled an user
--
create procedure CK.sUserEnabled 
	@UserId int
as begin

	if @UserId is not null 
	begin 

		select UserName from CK.tUser where UserId = @UserId;

		if @@RowCount = 1 update CK.tUser set IsEnabled = 1 where UserId = @UserId;

	end
	
	return 0;
end