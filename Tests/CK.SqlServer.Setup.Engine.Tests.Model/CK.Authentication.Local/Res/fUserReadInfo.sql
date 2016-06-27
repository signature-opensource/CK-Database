-- SetupConfig: {}
-- sUserReadInfo: 
--	When @Pwd is null it is not controlled. If it is empty or set to any string, it is controlled.
-- Returns:
--	- A row containing UserID, CryptedPassword, IsSysAdmin, PreferredCulture and ApplicationId.
--	Always fallback to anonymous if user name not found or if password does not match.
alter function CK.fUserReadInfo
(
    @UserName NVarChar(64),
    --@AuthenticatedActorId Int,
    @Pwd NVarChar(48),
    @TrackAccess Bit
)
returns @T table 
(
	UserId int not null primary key, 
	UserName nvarchar(64) not null,
	ApplicationId int not null,
	PreferredCulture int not null,	
	CryptedPassword bit not null,
	IsSysAdmin bit not null
)
as 
begin
	declare @UserId int;
	declare @uPwd nvarchar(48);
	declare @cryptedPassword bit;
	declare @isSysAdmin bit;
	declare @UserKey int;
	declare @preferredCulture int;
	declare @applicationId int;
	set @isSysAdmin = 0;
	set @cryptedPassword = 0;
	
	declare @idU int;
	-- First, read user with userName.
	select @idU = UserId, @uPwd = u.Pwd, @cryptedPassword = u.CryptedPassword --, @UserKey = UserAnonymousID
		from CK.tUser u with(nolock)
		where UserName = @UserName --collate Latin1_General_BIN;

	--if (@@rowcount = 0 or @idU = 0) and @UserId > 0
	--begin
	--	select @UserName = UserName, @uPwd = u.Pwd, @cryptedPassword = u.CryptedPassword --, @UserKey = UserAnonymousID
	--		from CK.tUser u with(nolock)
	--		where UserId = @UserId;
	--	if @@rowcount > 0 set @idU = @UserId
	--end

	if @cryptedPassword = 1  -- CryptedPassword
	begin
		set @Pwd = CK.fCukeHashPassword( @Pwd, @idU );
	end
		
	-- If the user is found and PWD is null, rehydrate the context only
	-- If the user is found and PWD is not null, challenge the PWD
	-- If the user is not found, give up.
	if (@idU is null or @idU = 0) or (@Pwd is not null and @Pwd <> @uPwd )
	begin
		set @UserId = 0;
	end			
	else 
	begin
		set @UserId = @idU;
	end
	-- <OnUserIdSet />
	
	if @TrackAccess = 1
	begin
		declare @LastAccessUTCTime datetime; -- Used in OnLastAccessSet fragment.
		--<OnLastAccessSet /> 
	end
	
	if @UserId = 1 
	begin
		set @isSysAdmin = 1;
	end
	--<OnIsSysAdminSet /> 

	-- CKCore.tSystem.DefaultLCID is (currently) added by Resource package.
	--select @preferredCulture = DefaultLCID from CKCore.tSystem; -- Default LCID
	set @preferredCulture = 12;
	--<OnPreferredCultureSet />

	set @applicationId = 0; -- Default ApplicationId
	--<OnApplicationIdSet />

	--<PostUserReadInfo />

	insert @T 
		select @UserId, @UserName, @applicationId, @preferredCulture, @cryptedPassword, @isSysAdmin  --, @UserKey;

	-- Another select of {Group names} or {Roles} may follow:
	-- this mimics classical user/role implementation and hence can be used to easily bind to a legacy user/groups 
	-- definition since these groups will be available in the ActorPrincipal.ExtraRoles collection and hence be 
	-- easily challenged.
    --<UserReadRoles />
    
    return 
end