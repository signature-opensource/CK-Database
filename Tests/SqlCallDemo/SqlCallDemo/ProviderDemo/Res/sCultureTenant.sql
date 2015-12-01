-- Version = 1.0.0
create procedure sCultureTenant
(
    @CultureId int,
    @TenantId int,
	@TextResult nvarchar(128) output
)
as
begin

	if @CultureId is null set @TextResult = N'@CultureId is null';
	else set @TextResult = N'@CultureId = ' + cast( @CultureId as nvarchar );

	if @TenantId is null set @TextResult = @TextResult + N', @TenantId is null';
	else set @TextResult = @TextResult + N', @TenantId = ' + cast( @TenantId as nvarchar );

	return 0;
end

