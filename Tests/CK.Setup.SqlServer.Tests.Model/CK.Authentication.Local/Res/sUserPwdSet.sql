-- Version = 1.0.0, Requires={ CK.fCukeHashPassword }
create procedure CK.sUserPwdSet
(
    @ActorId int,
    @UserId int,
    @PwdResult nvarchar(48) output
)
as begin
  
	set nocount on;
    --<PreSet /> 
	if exists( select * from CK.tUser where UserId = @UserId and CryptedPassword = 1 ) -- CryptedPassword
    begin
		set @PwdResult = CK.fCukeHashPassword( @PwdResult, @UserId );
	end
	update u 
		set u.Pwd = @PwdResult  
        from CK.tUser u   
        where u.UserId = @UserId;

    --<PostSet reverse="true" />
	 
	return 0;
end