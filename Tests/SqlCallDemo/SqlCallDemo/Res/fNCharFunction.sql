-- SetupConfig: {}
create function CK.fNCharFunction( @C nchar ) returns nchar
as
begin
	if @C is null return N'~';
	return @C;
end

