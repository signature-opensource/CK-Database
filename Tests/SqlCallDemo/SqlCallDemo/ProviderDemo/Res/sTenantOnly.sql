-- Version = 1.0.0
create procedure sTenantOnly
(
    @TenantId int,
	@TextResult nvarchar(128) output
)
as
begin
	if @TenantId is null set @TextResult = N'@TenantId is null';
	else set @TextResult = N'@TenantId = ' + cast( @TenantId as nvarchar );
	return 0;
end

