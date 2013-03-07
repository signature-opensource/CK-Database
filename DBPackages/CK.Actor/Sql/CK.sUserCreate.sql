-- Version = 1.1.0, Package = CK.Actor
--
-- Finds or creates an user.
--
create procedure CK.sUserCreate 
(
	@ActorId int, -- ActorId executing the stored procedure
	@UserName nvarchar ( 32 ),
	@Email varchar ( 100 ),
    @MustBeUnique bit,
	@UserIdResult int output
)
as begin

	-- <PreExecute /> : Check @ActorId / Checks user exists / 
	
	select @UserIdResult = UserId from CK.tUser where UserName = @UserName;
	if @@RowCount = 0
	begin
		-- <PreCreate />
		exec CK.sActorCreate @ActorId, @UserIdResult output;
		insert into CK.tUser ( UserId, UserName, Email ) VALUES ( @UserIdResult, @UserName, @Email );
		-- <PostCreate />
	end
    else if @MustBeUnique = 1
    begin
        set @UserIdResult = 0;
    end
	
	-- <PostExecute />
	
	return 0;
end