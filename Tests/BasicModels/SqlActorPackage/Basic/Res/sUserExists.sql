-- SetupConfig : {}
create procedure CK.sUserExists 
(
	@UserName nvarchar( 255 ),
	@ExistsResult bit output
)
as
begin
	--[beginsp]
	if exists( select * from CK.tUser where UserName = @UserName )
		set @ExistsResult = 1;
	else set @ExistsResult = 0;
	--[endsp]
end

