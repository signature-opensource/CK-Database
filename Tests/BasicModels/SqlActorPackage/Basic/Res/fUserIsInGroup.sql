-- SetupConfig: {}
create function CK.fUserIsInGroup
(
	@UserId int,
	@GroupId int
)
returns bit with schemabinding
as 
begin
	return case when exists( select 1 from CK.tActorProfile where ActorId = @UserId and GroupId = @GroupId ) then 1 else 0 end;
end

