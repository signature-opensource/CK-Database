-- SetupConfig: {}
create procedure sProtoUserCreate
(
 @ActorId int, 
 @UserName nvarchar(255),
 @Email nvarchar(255), 
 @Phone varchar(42), 
 @ProtoUserId int output
)
as
begin
	if @Email is null or  CHARINDEX('@', @Email) <= 0 throw 50000, 'Security.EmailIsInvalid', 1;
	--[beginsp]
	--insert into IV.tProtoUser( UserName, Email, Phone ) values ( @UserName, @Email, @Phone );
	--set @ProtoUserId = SCOPE_IDENTITY(); 
	set @ProtoUserId = @ActorId + len(@UserName); 
	--[endsp]
end