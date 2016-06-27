-- SetupConfig: {}
create function CK.fStringFunction( @V int ) returns nvarchar(60)
as
begin
	if @V is null return N'@V is null';
	return N'@V = ' + cast( @V as nvarchar );
end

