-- Version = 3.7.1
create procedure CK.sStringReturn
(
    @V int,
	@TextResult nvarchar(128) output
)
as
begin
	if @V is null set @TextResult = N'@V is null';
	else set @TextResult = N'@V = ' + cast( @V as nvarchar );
	return 0;
end

