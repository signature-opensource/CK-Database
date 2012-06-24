create procedure CK.sFirtProc
	@I int,
	@J int,
	@K int output
as
begin
	--[beginsp]
	set @K = @I * @J;
	--[endsp]
end