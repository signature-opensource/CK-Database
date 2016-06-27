-- SetupConfig: {}
create procedure CK.sOutputParameterWithDefault
(
	-- This emits a warning during DBSetup: if a pure output parameter has a default value then 
	-- it should be marked /* i n p u t */output since the i n p u t value seems to matter.
    @TextResult nvarchar(128) = N'The Sql Default.' output
)
as
begin
	if @TextResult is null set @TextResult = N'NULL input for @TextResult!';
	set @TextResult = N'Return: ' + @TextResult;
	return 0;
end

