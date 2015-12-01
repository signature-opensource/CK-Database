-- Version = 3.7.1
create procedure CK.sOutputInputParameterWithDefault
(
    @TextResult nvarchar(128) = N'The Sql Default.' /*input*/output
)
as
begin
	set @TextResult = N'Return: ' + @TextResult;
	return 0;
end

