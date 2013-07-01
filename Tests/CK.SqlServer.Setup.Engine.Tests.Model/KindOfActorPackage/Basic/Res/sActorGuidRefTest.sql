-- Version = 3.7.1
create procedure CK.sActorGuidRefTest
(
	@InOnly uniqueidentifier,
	@InAndOut uniqueidentifier /*input*/output,
	@TextResult nvarchar(128) output
)
as
begin
	--[beginsp]
	if @InOnly is null set @TextResult = N'@InOnly is null';
	else set @TextResult = N'@InOnly is not null';
	
	if @InAndOut is null set @TextResult = @TextResult + N', @InAndOut is null.';
	else set @TextResult = @TextResult + N', @InAndOut is not null.';

	if @InOnly is null set @InAndOut = null;
	else set @InAndOut = newid();
	
	--[endsp]
end

