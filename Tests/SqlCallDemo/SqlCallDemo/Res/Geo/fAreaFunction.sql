-- SetupConfig: {}
create function CK.fAreaFunction( @G Geography ) returns float
as
begin
	if @G is null return -1.0;
	return @G.STArea();
end

