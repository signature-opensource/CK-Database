-- Version = 3.7.1
create procedure CK.sIntReturn
(
    @V int,
	@Result int output
)
as
begin
	if @V is null set @Result = -1;
	else set @Result = @V*@V;
	return 0;
end

