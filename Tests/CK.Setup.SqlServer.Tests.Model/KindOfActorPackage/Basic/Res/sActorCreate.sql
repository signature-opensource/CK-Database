-- Version = 2.12.2
create procedure CK.sActorCreate 
(
	@ActorIdResult int output
)
as
begin
	--[beginsp]
	insert into CK.tActor default values;
	set @ActorIdResult = SCOPE_IDENTITY();
	--[endsp]
end

