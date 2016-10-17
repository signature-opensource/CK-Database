-- SetupConfig: {}
alter function CK.fByteFunction( @V int ) returns tinyint
as
begin
	if @V is null return 0;
	if @V = -1 return null;
	return cast( @V * @V as tinyint);
end

