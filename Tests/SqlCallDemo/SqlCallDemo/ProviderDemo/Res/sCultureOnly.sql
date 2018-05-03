-- SetupConfig: {}
create procedure sCultureOnly
(
    @CultureId int,
	@TextResult nvarchar(128) output
)
as
begin
	if @CultureId is null set @TextResult = N'@CultureId is null';
	else set @TextResult = N'@CultureId = ' + cast( @CultureId as nvarchar );
	return 0;
end

