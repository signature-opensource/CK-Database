-- Version = *
create procedure CK.sSimpleReplaceTest
(
    @TextParam nvarchar(128) = N'The Sql Default.' /*input*/output,
	@Added int = 0
)
as
begin
	set @TextParam = N'Return: ' + @TextParam + N' ' + cast(@Added as nvarchar);
	return 0;
end

