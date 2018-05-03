-- SetupConfig: {}
create procedure CK.sWithEnumIO( 
	@BytePower tinyint, 
	@Power int /*input*/output 
)
as
begin
	set @Power = @BytePower + @Power;
	return 0;
end

