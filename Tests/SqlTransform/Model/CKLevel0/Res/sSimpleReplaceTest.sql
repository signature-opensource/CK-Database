-- Version = *
create procedure CK.sSimpleReplaceTest
(
    @TextParam nvarchar(128) = N'The Sql Default.' /*input*/output
)
as
begin
	set @TextParam = N'Return: ' + @TextParam;
	return 0;
end

