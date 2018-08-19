-- SetupConfig: {}
create function CK.fCharFunction( @C char ) returns char
as
begin
	if @C is null return '~';
	return @C;
end

