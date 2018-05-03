-- SetupConfig: {}
create procedure CK.sGuidRefTest
(
    @ReplaceInAndOut bit,
	@InOnly uniqueidentifier,
	@InAndOut uniqueidentifier /*input*/output,
	@TextResult nvarchar(128) output
)
as
begin

	if @InOnly is null set @TextResult = N'@InOnly is null';
	else set @TextResult = N'@InOnly is not null';
	
	if @InAndOut is null set @TextResult = @TextResult + N', @InAndOut is null.';
	else set @TextResult = @TextResult + N', @InAndOut is not null.';

	if @ReplaceInAndOut = 1 set @InAndOut = newid();

	return 0;
end

