-- SetupConfig : { "Requires" : [ "CK.sActorCreate" ] } 
create procedure CK.sUserCreate 
(
	@UserName nvarchar( 255 ),
	@UserIdResult int output
)
as
begin
	--[beginsp]
	select @UserIdResult = UserId from CK.tUser where UserName = @UserName;
	if @@rowcount = 0
	begin
		exec CK.sActorCreate @UserIdResult output;
		insert into CK.tUser( UserId, UserName, Email ) values ( @UserIdResult, @UserName, '' );
	end
	--[endsp]
end

