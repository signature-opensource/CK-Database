-- SetupConfig : {}
create procedure CK.sActorCreate 
(
	@ActorIdResult int output
)
as
begin
	--[beginsp]
	insert into CK.tActor default values;
	set @ActorIdResult = SCOPE_IDENTITY();
	insert into CK.tActorProfile( ActorId, GroupId ) values( @ActorIdResult, @ActorIdResult );
	--[endsp]
end

