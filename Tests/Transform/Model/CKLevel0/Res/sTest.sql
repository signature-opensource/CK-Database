-- Version = *
create procedure CK.sTest
(
    @TextParam nvarchar(128) = N'The Sql Default.' /*input*/output
)
as
begin
	set @TextParam = N'Return: ' + @TextParam;
	return 0;
end

