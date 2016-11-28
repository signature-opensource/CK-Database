-- SetupConfig: {}
create procedure CK.sWithImplicitConversionIO( 
	@Power tinyint, 
	@Scopes varchar(500),
	@Result varchar(1000) output
)
as
begin
	set @Result = @Power + ' - ' + @Scopes;
	return 0;
end

