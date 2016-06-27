-- SetupConfig: {}
create procedure CK.sUserPwdEncrypt
(
    @ActorId int,
    @UserId int
)
as begin
  
	set nocount on;
	-- Checks wether the @ActorId can Encrypt the password of the user.
    --<PreSet /> 
	-- Compute a hash of the current password
	-- Updates the CryptedPassword field to 1
	-- Sets the new password to a crypted version
    --<PostSet reverse="true" />
	 
	return 0;
end